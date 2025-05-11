using System.Collections.Concurrent;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Manages authentication to AWS IAM to obtain access tokens.
/// </summary>
/// <remarks>
/// Implements <see cref="AWSCredentials"/> and <see cref="ICredential"/>.
/// Caches, refreshes, and provides status reporting for tokens.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="awsCredentialsManager">The AWS credentials manager.</param>
internal class ManagedCredential(
    ILogger logger,
    AWSCredentialsManager awsCredentialsManager)
    : AWSCredentials, ICredential
{
    #region Private Fields

    /// <summary>
    /// Thread-safe cache of token items by scope.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Lazy{T}"/> to ensure thread-safe token acquisition.
    /// Each <see cref="AmazonTokenItem"/> handles its own refresh scheduling.
    /// </remarks>
    private readonly ConcurrentDictionary<string, Lazy<AmazonTokenItem>> _amazonTokenItemsByScope = new();

    #endregion

    #region AWSCredentials

    /// <inheritdoc />
    public override ImmutableCredentials GetCredentials() => awsCredentialsManager.AWSCredentials.GetCredentials();

    #endregion AWSCredentials

    #region ICredential

    /// <summary>
    /// Gets a Trelnex access token for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>A Trelnex <see cref="AccessToken"/> for the specified scope.</returns>
    /// <exception cref="AccessTokenUnavailableException">Thrown when the credential cannot retrieve a token.</exception>
    /// <remarks>
    /// Implements <see cref="ICredential.GetAccessToken"/>.
    /// Tokens are cached by scope and refreshed automatically.
    /// </remarks>
    public AccessToken GetAccessToken(
        string scope)
    {
        // Get or create a token item for this scope.
        // Using Lazy<T> ensures thread safety during creation.
        // See: https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyAmazonTokenItem =
            _amazonTokenItemsByScope.GetOrAdd(
                key: scope,
                value: new Lazy<AmazonTokenItem>(
                    AmazonTokenItem.Create(
                        logger,
                        awsCredentialsManager,
                        scope)));

        // Return the token from the token item.
        return lazyAmazonTokenItem.Value.GetAccessToken();
    }

    /// <summary>
    /// Gets the status of all credentials managed by this provider.
    /// </summary>
    /// <returns>A <see cref="CredentialStatus"/> object containing the status of all managed tokens.</returns>
    /// <remarks>
    /// Implements <see cref="ICredential.GetStatus"/>.
    /// Collects status information from all token items in the cache.
    /// </remarks>
    public CredentialStatus GetStatus()
    {
        // Collect status of all token items in the cache.
        var statuses = _amazonTokenItemsByScope
            .Select(kvp =>
            {
                var lazy = kvp.Value;
                var amazonTokenItem = lazy.Value;

                return amazonTokenItem.GetStatus();
            })
            .OrderBy(status => string.Join(", ", status.Scopes))
            .ToArray();

        // Return a consolidated credential status with all token statuses.
        return new CredentialStatus(
            Statuses: statuses ?? []);
    }

#endregion ICredential

    #region AmazonTokenItem

    /// <summary>
    /// Manages an AWS access token with automatic refresh.
    /// </summary>
    private class AmazonTokenItem
    {
        #region Private Fields

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The AWS credentials manager.
        /// </summary>
        private readonly AWSCredentialsManager _awsCredentialsManager;

        /// <summary>
        /// The scope of the access token.
        /// </summary>
        private readonly string _scope;

        /// <summary>
        /// The timer used to schedule token refresh.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// The current access token.
        /// </summary>
        private AccessToken? _accessToken;

        /// <summary>
        /// The error message if the token is unavailable.
        /// </summary>
        private string? _unavailableMessage;

        /// <summary>
        /// The inner exception if the token is unavailable.
        /// </summary>
        private Exception? _unavailableInnerException;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonTokenItem"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="awsCredentialsManager">The AWS credentials manager.</param>
        /// <param name="scope">The scope of the access token.</param>
        private AmazonTokenItem(
            ILogger logger,
            AWSCredentialsManager awsCredentialsManager,
            string scope)
        {
            _logger = logger;
            _awsCredentialsManager = awsCredentialsManager;
            _scope = scope;

            // Create a timer but don't start it yet.
            _timer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates and initializes a new <see cref="AmazonTokenItem"/> instance.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="awsCredentialsManager">The AWS credentials manager.</param>
        /// <param name="scope">The scope of the access token.</param>
        /// <returns>A new <see cref="AmazonTokenItem"/> instance with an initial token.</returns>
        /// <remarks>Creates the token item and triggers an initial token refresh.</remarks>
        public static AmazonTokenItem Create(
            ILogger logger,
            AWSCredentialsManager awsCredentialsManager,
            string scope)
        {
            // Create the token item.
            var amazonTokenItem = new AmazonTokenItem(
                logger,
                awsCredentialsManager,
                scope);

            // Immediately trigger a refresh to acquire the initial token.
            amazonTokenItem.Refresh(null);

            return amazonTokenItem;
        }

        /// <summary>
        /// Gets the current access token.
        /// </summary>
        /// <returns>The current access token.</returns>
        /// <exception cref="AccessTokenUnavailableException">Thrown when no valid token is available.</exception>
        public AccessToken GetAccessToken()
        {
            lock (this)
            {
                // If no token is available, throw an exception with the error details.
                return _accessToken ?? throw new AccessTokenUnavailableException(_unavailableMessage, _unavailableInnerException);
            }
        }

        /// <summary>
        /// Gets the status of the access token managed by this item.
        /// </summary>
        /// <returns>An <see cref="AccessTokenStatus"/> containing the token's health and metadata.</returns>
        public AccessTokenStatus GetStatus()
        {
            lock (this)
            {
                // Determine if the token is valid based on its expiration time.
                var health = ((_accessToken?.ExpiresOn ?? DateTimeOffset.MinValue) < DateTimeOffset.UtcNow)
                    ? AccessTokenHealth.Expired
                    : AccessTokenHealth.Valid;

                // Return a status object with all relevant information.
                return new AccessTokenStatus(
                    Health: health,
                    Scopes: [ _scope ],
                    ExpiresOn: _accessToken?.ExpiresOn);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Refreshes the access token and schedules the next refresh.
        /// </summary>
        /// <param name="state">The state object passed by the Timer (not used).</param>
        private void Refresh(
            object? state)
        {
            // Log the refresh attempt.
            _logger.LogInformation(
                "AmazonTokenItem.Refresh: scope = '{scope:l}'",
                _scope);

            // Default to refreshing in 5 seconds if something goes wrong.
            var dueTime = TimeSpan.FromSeconds(5);

            try
            {
                // Attempt to get a new token.
                var accessToken = _awsCredentialsManager.GetAccessToken(_scope).GetAwaiter().GetResult();

                // Store the new token.
                SetAccessToken(accessToken);

                // Determine when to refresh the token.
                var refreshOn = accessToken.RefreshOn ?? accessToken.ExpiresOn;

                // Log the successful token acquisition.
                _logger.LogInformation(
                    "AmazonTokenItem.AccessToken: scope = '{scope:l}', refreshOn = '{refreshOn:o}'.",
                    _scope,
                    refreshOn);

                // Schedule the next refresh at the token's refresh time.
                dueTime = refreshOn - DateTimeOffset.UtcNow;
            }
            catch (HttpStatusCodeException ex)
            {
                // Handle token unavailable errors.
                SetUnavailable(ex);

                // Log the error.
                _logger.LogError(
                    "AmazonTokenItem.Unavailable: scope = '{scope:l}', message = '{message:}'.",
                    _scope,
                    ex.Message);
            }
            catch (Exception ex)
            {
                // Catch any other exceptions to ensure the timer is always rescheduled.
                _logger.LogError(
                    "AmazonTokenItem.Error: scope = '{scope:l}', message = '{message:}'.",
                    _scope,
                    ex.Message);
            }

            // Schedule the next refresh.
            _timer.Change(
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Sets a new access token and clears any error state.
        /// </summary>
        /// <param name="accessToken">The new access token.</param>
        private void SetAccessToken(
            AccessToken accessToken)
        {
            lock (this)
            {
                // Store the new token.
                _accessToken = accessToken;

                // Clear any error state.
                _unavailableMessage = null;
                _unavailableInnerException = null;
            }
        }

        /// <summary>
        /// Sets the error information to use when the token is unavailable.
        /// </summary>
        /// <param name="ex">The exception that occurred during token acquisition.</param>
        private void SetUnavailable(
            HttpStatusCodeException exception)
        {
            lock (this)
            {
                // Store the error information.
                _unavailableMessage = exception.Message;
                _unavailableInnerException = exception.InnerException;
            }
        }

        #endregion
    }

    #endregion AmazonTokenItem
}
