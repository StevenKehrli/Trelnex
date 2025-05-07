using System.Collections.Concurrent;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Enables authentication to AWS IAM to obtain an access token.
/// </summary>
/// <remarks>
/// <para>
/// ManagedCredential is an internal class. Users will get an instance of the <see cref="AWSCredentials"/> class.
/// </para>
/// </remarks>
/// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
/// <param name="awsCredentialsManager">The <see cref="AWSCredentialsManager"/> to manage the <see cref="AWSCredentials"/> and get the <see cref="Trelnex.Core.Identity.AccessToken"/> from the Amazon /oauth2/token endpoint.</param>
internal class ManagedCredential(
    ILogger logger,
    AWSCredentialsManager awsCredentialsManager)
    : AWSCredentials, ICredential
{

    /// <summary>
    /// A thread-safe collection of scope to <see cref="AmazonTokenItem"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<AmazonTokenItem>> _amazonTokenItemsByTokenRequestContextKey = new();

#region AWSCredentials

    public override ImmutableCredentials GetCredentials() => awsCredentialsManager.AWSCredentials.GetCredentials();

#endregion AWSCredentials

#region ICredential

    /// <summary>
    /// Gets the <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>The <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.</returns>
    public AccessToken GetAccessToken(
        string scope)
    {
        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyAmazonTokenItem =
            _amazonTokenItemsByTokenRequestContextKey.GetOrAdd(
                key: scope,
                value: new Lazy<AmazonTokenItem>(
                    AmazonTokenItem.Create(
                        logger,
                        awsCredentialsManager,
                        scope)));


        // get the access token
        return lazyAmazonTokenItem.Value.GetAccessToken();
    }

    /// <summary>
    /// Gets the <see cref="CredentialStatus"/> of the credential used by this token provider.
    /// </summary>
    /// <returns>The <see cref="CredentialStatus"/> of the credential used by this token provider.</returns>
    public CredentialStatus GetStatus()
    {
        // get the amazon token item
        var statuses = _amazonTokenItemsByTokenRequestContextKey
            .Select(kvp =>
            {
                var lazy = kvp.Value;
                var amazonTokenItem = lazy.Value;

                return amazonTokenItem.GetStatus();
            })
            .OrderBy(status => string.Join(", ", status.Scopes))
            .ToArray();

        return new CredentialStatus(
            Statuses: statuses ?? []);
    }

#endregion

    /// <summary>
    /// Combines an <see cref="Trelnex.Core.Identity.AccessToken"/> with a <see cref="System.Threading.Timer"/> to refresh the <see cref="Trelnex.Core.Identity.AccessToken"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An AmazonTokenItem will refresh its token in accordance with <see cref="Trelnex.Core.Identity.AccessToken.RefreshOn"/>.
    /// This will maintain a valid token and enable <see cref="GetToken"/> to return immediately.
    /// </para>
    /// </remarks>
    private class AmazonTokenItem
    {
        /// <summary>
        /// The <see cref="ILogger"> used to perform logging.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The <see cref="AWSCredentialsManager"/> to manage the <see cref="AWSCredentials"/> and get the <see cref="Trelnex.Core.Identity.AccessToken"/> from the Amazon /oauth2/token endpoint.
        /// </summary>
        private readonly AWSCredentialsManager _awsCredentialsManager;

        /// <summary>
        /// The scope of the <see cref="AccessToken"/>.
        private readonly string _scope;

        /// <summary>
        /// The <see cref="Timer"/> to refresh the <see cref="Trelnex.Core.Identity.AccessToken"/>.
        /// </summary>
        private readonly Timer _timer;

        /// <summary>
        /// The underlying <see cref="Trelnex.Core.Identity.AccessToken"/>.
        /// </summary>
        private AccessToken? _accessToken;

        /// <summary>
        /// The message to throw when the token is unavailable.
        /// </summary>
        private string? _unavailableMessage;

        /// <summary>
        /// The inner exception to include when the token is unavailable.
        /// </summary>
        private Exception? _unavailableInnerException;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonTokenItem"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
        /// <param name="awsCredentialsManager">The <see cref="AWSCredentialsManager"/> to manage the <see cref="AWSCredentials"/> and get the <see cref="Trelnex.Core.Identity.AccessToken"/> from the Amazon /oauth2/token endpoint.</param>
        /// <param name="scope">The scope of the <see cref="AccessToken"/>.</param>
        private AmazonTokenItem(
            ILogger logger,
            AWSCredentialsManager awsCredentialsManager,
            string scope)
        {
            _logger = logger;
            _awsCredentialsManager = awsCredentialsManager;
            _scope = scope;

            _timer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonTokenItem"/>.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"> used to perform logging.</param>
        /// <param name="awsCredentialsManager">The <see cref="AWSCredentialsManager"/> to manage the <see cref="AWSCredentials"/> and get the <see cref="Trelnex.Core.Identity.AccessToken"/> from the Amazon /oauth2/token endpoint.</param>
        /// <param name="scope">The scope of the <see cref="AccessToken"/>.</param>
        public static AmazonTokenItem Create(
            ILogger logger,
            AWSCredentialsManager awsCredentialsManager,
            string scope)
        {
            // create the amazonTokenItem and schedule the refresh (to get its token)
            // this will set _accessToken
            var amazonTokenItem = new AmazonTokenItem(
                logger,
                awsCredentialsManager,
                scope);

            amazonTokenItem.Refresh(null);

            return amazonTokenItem;
        }

        /// <summary>
        /// Gets the <see cref="Trelnex.Core.Identity.AccessToken"/>.
        /// </summary>
        public AccessToken GetAccessToken()
        {
            lock (this)
            {
                return _accessToken ?? throw new AccessTokenUnavailableException(_unavailableMessage, _unavailableInnerException);
            }
        }

        /// <summary>
        /// Gets the <see cref="AccessTokenStatus"/> for this access token.
        /// </summary>
        /// <returns>A <see cref="AccessTokenStatus"/> describing the status of this access token.</returns>
        public AccessTokenStatus GetStatus()
        {
            lock (this)
            {
                var health = ((_accessToken?.ExpiresOn ?? DateTimeOffset.MinValue) < DateTimeOffset.UtcNow)
                    ? AccessTokenHealth.Expired
                    : AccessTokenHealth.Valid;

                return new AccessTokenStatus(
                    Health: health,
                    Scopes: [ _scope ],
                    ExpiresOn: _accessToken?.ExpiresOn);
            }
        }

        /// <summary>
        /// The <see cref="TimerCallback"/> delegate of the <see cref="_timer"/> <see cref="Timer"/>.
        /// </summary>
        /// <param name="state">An object containing application-specific information relevant to the method invoked by this delegate, or null.</param>
        private void Refresh(object? state)
        {
            _logger.LogInformation(
                "AmazonTokenItem.Refresh: scope = '{scope:l}'",
                _scope);

            // assume we need to refresh in 5 seconds
            var dueTime = TimeSpan.FromSeconds(5);

            try
            {
                // get a new token and set
                var accessToken = _awsCredentialsManager.GetAccessToken(_scope).GetAwaiter().GetResult();

                SetAccessToken(accessToken);

                // we got a token - schedule refresh
                var refreshOn = accessToken.RefreshOn ?? accessToken.ExpiresOn;

                _logger.LogInformation(
                    "AmazonTokenItem.AccessToken: scope = '{scope:l}', refreshOn = '{refreshOn:o}'.",
                    _scope,
                    refreshOn);

                dueTime = refreshOn - DateTimeOffset.UtcNow;
            }
            catch (HttpStatusCodeException ex)
            {
                SetUnavailable(ex);

                _logger.LogError(
                    "AmazonTokenItem.Unavailable: scope = '{scope:l}', message = '{message:}'.",
                    _scope,
                    ex.Message);
            }
            catch
            {
            }

            _timer.Change(
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Sets the <see cref="Trelnex.Core.Identity.AccessToken"/> for this object.
        /// </summary>
        private void SetAccessToken(
            AccessToken accessToken)
        {
            lock (this)
            {
                _accessToken = accessToken;

                _unavailableMessage = null;
                _unavailableInnerException = null;
            }
        }

        /// <summary>
        /// Sets the message to throw when the token is unavailable.
        /// </summary>
        /// <param name="ex">The <see cref="HttpStatusCodeException"/> with the message to throw when the token is unavailable.</param>
        private void SetUnavailable(
            HttpStatusCodeException ex)
        {
            lock (this)
            {
                _unavailableMessage = ex.Message;
                _unavailableInnerException = ex.InnerException;
            }
        }
    }
}
