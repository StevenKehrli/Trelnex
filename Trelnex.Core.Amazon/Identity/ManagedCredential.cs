using System.Collections.Concurrent;
using System.Diagnostics;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SecurityToken.Model.Internal.MarshallTransformations;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Manages AWS credentials and Trelnex access tokens with automatic refresh.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ICredential"/> to provide Trelnex access tokens for API authentication.
/// </para>
/// <para>
/// Uses AWS SigV4 signing to authenticate token requests with AWS caller identity.
/// Caches tokens by scope and automatically refreshes them before expiration.
/// </para>
/// </remarks>
internal class ManagedCredential : AWSCredentials, ICredential
{
    #region Private Fields

    /// <summary>
    /// Client for requesting access tokens.
    /// </summary>
    private readonly AccessTokenClient _accessTokenClient;

    /// <summary>
    /// Thread-safe cache of token items by scope.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Lazy{T}"/> to ensure thread-safe token acquisition.
    /// Each <see cref="AmazonTokenItem"/> handles its own refresh scheduling.
    /// </remarks>
    private readonly ConcurrentDictionary<string, Lazy<AmazonTokenItem>> _amazonTokenItemsByScope = new();

    /// <summary>
    /// The AWS credentials for signing requests.
    /// </summary>
    private readonly AWSCredentials _awsCredentials;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// AWS principal ARN.
    /// </summary>
    private readonly string _principalId;

    /// <summary>
    /// STS client for AWS identity operations.
    /// </summary>
    private readonly AmazonSecurityTokenServiceClient _stsClient;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedCredential"/> class.
    /// </summary>
    /// <param name="awsCredentials">The AWS credentials for signing requests.</param>
    /// <param name="stsClient">The STS client for AWS identity operations.</param>
    /// <param name="principalId">The AWS principal ARN.</param>
    /// <param name="accessTokenClient">The client for OAuth2 token requests.</param>
    /// <param name="logger">The logger.</param>
    private ManagedCredential(
        AWSCredentials awsCredentials,
        AmazonSecurityTokenServiceClient stsClient,
        string principalId,
        AccessTokenClient accessTokenClient,
        ILogger logger)
    {
        _awsCredentials = awsCredentials;
        _stsClient = stsClient;
        _principalId = principalId;
        _accessTokenClient = accessTokenClient;
        _logger = logger;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="ManagedCredential"/> instance with initialized AWS infrastructure.
    /// </summary>
    /// <param name="awsCredentials">The AWS credentials for signing requests.</param>
    /// <param name="options">Configuration options for region and access token client endpoint.</param>
    /// <param name="logger">The logger for recording operations.</param>
    /// <returns>A fully initialized <see cref="ManagedCredential"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// Initializes the AWS Security Token Service (STS) client with the specified region,
    /// retrieves the AWS caller identity (principal ARN), and configures the OAuth2 token client.
    /// </para>
    /// <para>
    /// The caller identity is used to authenticate access token requests via AWS SigV4 signatures.
    /// </para>
    /// </remarks>
    public static async Task<ManagedCredential> Create(
        AWSCredentials awsCredentials,
        AmazonCredentialOptions options,
        ILogger logger)
    {
        // Initialize the Security Token Service client with the specified region
        var regionEndpoint = RegionEndpoint.GetBySystemName(options.Region);

        var stsClientConfig = new AmazonSecurityTokenServiceConfig
        {
            RegionEndpoint = regionEndpoint,
        };

        var stsClient = new AmazonSecurityTokenServiceClient(awsCredentials, stsClientConfig);

        // Retrieve the AWS caller identity to obtain the principal ARN
        var request = new GetCallerIdentityRequest();
        var response = await stsClient.GetCallerIdentityAsync(request);

        logger.LogInformation(
            "ManagedCredential.Create: principalId = '{Arn:l}', region = '{region:l}'",
            response.Arn,
            options.Region);

        // Initialize the OAuth2 access token client with the configured endpoint
        var httpClient = new HttpClient(new SocketsHttpHandler(), disposeHandler: false)
        {
            BaseAddress = options.AccessTokenClient.BaseAddress
        };

        var accessTokenClient = new AccessTokenClient(httpClient);

        // Create and return the fully initialized credential manager
        return new ManagedCredential(
            awsCredentials,
            stsClient,
            response.Arn,
            accessTokenClient,
            logger);
    }

    #endregion

    #region AWSCredentials

    /// <inheritdoc />
    public override ImmutableCredentials GetCredentials() => _awsCredentials.GetCredentials();

    #endregion

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
                    new AmazonTokenItem(
                        this,
                        scope,
                        _logger)));

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
            .ToArray() ?? [];

        // Return a consolidated credential status with all token statuses.
        return new CredentialStatus(statuses);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Obtains a Trelnex access token for the specified scope using AWS SigV4 signed authentication.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>A Trelnex access token with the requested scope.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when the token request fails with an HTTP error.</exception>
    /// <remarks>
    /// <para>
    /// Creates an AWS GetCallerIdentity request, signs it with AWS SigV4, and uses the
    /// signature to authenticate the access token request to the Trelnex token endpoint.
    /// </para>
    /// <para>
    /// This method is called by <see cref="AmazonTokenItem"/> instances during token refresh cycles.
    /// </para>
    /// </remarks>
    private async Task<AccessToken> GetAccessTokenAsync(
        string scope)
    {
        // Create and marshal a GetCallerIdentity request for AWS SigV4 signing
        var request = new GetCallerIdentityRequest();
        var marshaller = new GetCallerIdentityRequestMarshaller();
        var marshalledRequest = marshaller.Marshall(request);

        // Determine the STS endpoint and configure the marshalled request
        var endpoint = _stsClient.DetermineServiceOperationEndpoint(request);
        marshalledRequest.Endpoint = new Uri(endpoint.URL);

        // Sign the request using AWS SigV4 with the managed AWS credentials
        var awsSigner = new AWS4Signer();
        awsSigner.Sign(
            request: marshalledRequest,
            clientConfig: _stsClient.Config,
            metrics: null,
            identity: _awsCredentials);

        // Extract the signature components needed for token authentication
        var signature = new CallerIdentitySignature
        {
            Region = _stsClient.Config.RegionEndpoint.SystemName,
            Headers = marshalledRequest.Headers
        };

        // Request the access token using the AWS principal identity and signature
        var accessToken = await _accessTokenClient.GetAccessToken(
            _principalId,
            signature,
            scope);

        return accessToken;
    }

    #endregion

    #region AmazonTokenItem

    /// <summary>
    /// Manages a Trelnex access token for a specific scope with automatic refresh scheduling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each instance manages a single token for one scope, handling refresh cycles
    /// to ensure the token remains valid before expiration.
    /// </para>
    /// <para>
    /// Uses a fire-and-forget async pattern with <see cref="Task.Delay"/> for scheduling refreshes.
    /// </para>
    /// </remarks>
    private class AmazonTokenItem
    {
        #region Private Fields

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The parent ManagedCredential instance for accessing signing methods.
        /// </summary>
        private readonly ManagedCredential _managedCredential;

        /// <summary>
        /// The scope of the access token.
        /// </summary>
        private readonly string _scope;

        /// <summary>
        /// The current access token.
        /// </summary>
        private AccessToken? _accessToken;

        /// <summary>
        /// The inner exception if the token is unavailable.
        /// </summary>
        private Exception? _unavailableInnerException;

        /// <summary>
        /// The error message if the token is unavailable.
        /// </summary>
        private string? _unavailableMessage;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AmazonTokenItem"/> class.
        /// </summary>
        /// <param name="managedCredential">The parent ManagedCredential instance.</param>
        /// <param name="scope">The scope of the access token.</param>
        /// <param name="logger">The logger.</param>
        public AmazonTokenItem(
            ManagedCredential managedCredential,
            string scope,
            ILogger logger)
        {
            _managedCredential = managedCredential;
            _scope = scope;
            _logger = logger;

            // Start the refresh loop in the background (fire and forget)
            _ = ScheduleRefreshTokenAsync();
        }

        #endregion

        #region Public Methods

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
        /// Acquires a new access token and calculates when the next refresh should occur.
        /// </summary>
        /// <returns>The time span to wait before the next refresh attempt.</returns>
        /// <remarks>
        /// <para>
        /// On success, stores the token and returns the duration until the token's refresh time.
        /// On failure, stores the error and returns a 5-second retry delay.
        /// </para>
        /// <para>
        /// Exceptions are caught to ensure the refresh loop continues even after failures.
        /// </para>
        /// </remarks>
        private async Task<TimeSpan> RefreshTokenAsync()
        {
            // Default to retrying in 5 seconds on errors
            var dueTime = TimeSpan.FromSeconds(5);

            try
            {
                // Request a new access token using AWS SigV4 authentication
                var accessToken = await _managedCredential.GetAccessTokenAsync(_scope);

                // Store the successfully acquired token
                SetAccessToken(accessToken);

                // Use the token's refresh time, falling back to expiration time
                var refreshOn = accessToken.RefreshOn ?? accessToken.ExpiresOn;

                // Log successful token acquisition with next refresh time
                _logger.LogInformation(
                    "ManagedCredential.AmazonTokenItem.RefreshTokenAsync: scope = '{scope:l}', refreshOn = '{refreshOn:o}'.",
                    _scope,
                    refreshOn);

                // Calculate when to attempt the next refresh
                dueTime = refreshOn - DateTimeOffset.UtcNow;
            }
            catch (HttpStatusCodeException ex)
            {
                // Store error details for GetAccessToken to throw
                SetUnavailable(ex);

                // Log HTTP-level token acquisition failures
                _logger.LogError(
                    "ManagedCredential.AmazonTokenItem.Unavailable: scope = '{scope:l}', message = '{message:}'.",
                    _scope,
                    ex.Message);
            }
            catch (Exception ex)
            {
                // Log unexpected errors but continue the refresh loop
                _logger.LogError(
                    "ManagedCredential.AmazonTokenItem.Exception: scope = '{scope:l}', message = '{message:}'.",
                    _scope,
                    ex.Message);
            }

            return dueTime;
        }

        /// <summary>
        /// Orchestrates the token refresh cycle with timing and recursion.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Calls <see cref="RefreshTokenAsync"/> to acquire a new token, waits for the calculated delay,
        /// then recursively schedules the next refresh using fire-and-forget pattern.
        /// </para>
        /// <para>
        /// Logs timing information to track refresh performance.
        /// </para>
        /// </remarks>
        private async Task ScheduleRefreshTokenAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Log the start of the refresh cycle
            _logger.LogInformation(
                "ManagedCredential.AmazonTokenItem.ScheduleRefreshTokenAsync: scope = '{scope:l}'",
                _scope);

            // Perform the token refresh and get the next refresh delay
            var dueTime = await RefreshTokenAsync();

            stopwatch.Stop();
            _logger.LogInformation(
                "ManagedCredential.AmazonTokenItem.ScheduleRefreshTokenAsync: scope = '{scope:l}', elapsedMilliseconds = {elapsedMilliseconds} ms.",
                _scope,
                stopwatch.ElapsedMilliseconds);

            // Wait for the calculated delay before the next refresh
            await Task.Delay(dueTime);

            // Schedule the next refresh cycle (fire and forget)
            _ = ScheduleRefreshTokenAsync();
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

    #endregion
}
