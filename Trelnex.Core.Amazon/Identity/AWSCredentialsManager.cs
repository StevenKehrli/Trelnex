using System.Diagnostics;
using System.Reflection;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Amazon.Runtime.Internal.Auth;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SecurityToken.Model.Internal.MarshallTransformations;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Manages AWS credentials and provides access tokens.
/// </summary>
/// <remarks>
/// Initializes credentials, creates SigV4 signatures, acquires access tokens, and refreshes credentials.
/// Addresses an issue in <see cref="RefreshingAWSCredentials"/> by proactively refreshing credentials.
/// </remarks>
public class AWSCredentialsManager
{
    #region Private Fields

    /// <summary>
    /// Client for requesting access tokens.
    /// </summary>
    private readonly AccessTokenClient _accessTokenClient;

    /// <summary>
    /// STS client for AWS identity operations.
    /// </summary>
    private readonly AmazonSecurityTokenServiceClient _stsClient;

    /// <summary>
    /// AWS principal ARN.
    /// </summary>
    private readonly string _principalId;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AWSCredentialsManager"/> class.
    /// </summary>
    /// <param name="accessTokenClient">The client for OAuth2 token requests.</param>
    /// <param name="stsClient">The STS client for AWS identity operations.</param>
    /// <param name="principalId">The AWS principal ARN.</param>
    private AWSCredentialsManager(
        AccessTokenClient accessTokenClient,
        AmazonSecurityTokenServiceClient stsClient,
        string principalId)
    {
        _accessTokenClient = accessTokenClient;
        _stsClient = stsClient;
        _principalId = principalId;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the AWS credentials.
    /// </summary>
    public AWSCredentials AWSCredentials => _stsClient.Config.DefaultAWSCredentials;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="AWSCredentialsManager"/> with initialized AWS credentials.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options that configure AWS credentials and token client.</param>
    /// <returns>A fully initialized <see cref="AWSCredentialsManager.</returns>
    /// <remarks>
    /// Creates AWS credentials, initializes an STS client, obtains the caller identity, and creates the OAuth2 token client.
    /// </remarks>
    public static async Task<AWSCredentialsManager> Create(
        ILogger logger,
        AmazonCredentialOptions options)
    {
        // Create the AWS credentials with automatic refresh
        var awsCredentials = CreateAWSCredentials(logger);

        // Create the Security Token Service client with the specified region
        var regionEndpoint = RegionEndpoint.GetBySystemName(options.Region);

        var stsClientConfig = new AmazonSecurityTokenServiceConfig
        {
            RegionEndpoint = regionEndpoint,
        };

        var stsClient = new AmazonSecurityTokenServiceClient(awsCredentials, stsClientConfig);

        // Get the caller identity (AWS principal)
        var request = new GetCallerIdentityRequest();
        var response = await stsClient.GetCallerIdentityAsync(request);

        logger.LogInformation(
            "AWSCredentialsManager: principalId = '{Arn:l}', region = '{region:l}'",
            response.Arn,
            options.Region);

        // Create the OAuth2 token client
        var httpClient = new HttpClient(new SocketsHttpHandler(), disposeHandler: false)
        {
            BaseAddress = options.AccessTokenClient.BaseAddress
        };

        var accessTokenClient = new AccessTokenClient(httpClient);

        // Create and return the credentials manager
        return new AWSCredentialsManager(
            accessTokenClient,
            stsClient,
            response.Arn);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets an access token for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>An access token with the requested scope.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when the token request fails.</exception>
    /// <remarks>
    /// Creates a caller identity request, signs it using AWS SigV4, and requests an access token.
    /// </remarks>
    public async Task<AccessToken> GetAccessToken(
        string scope)
    {
        // Marshal the request to the AWS IRequest format
        var request = new GetCallerIdentityRequest();
        var marshaller = new GetCallerIdentityRequestMarshaller();
        var marshalledRequest = marshaller.Marshall(request);

        // Get the endpoint for the STS client and set it on the request
        var endpoint = _stsClient.DetermineServiceOperationEndpoint(request);
        marshalledRequest.Endpoint = new Uri(endpoint.URL);

        // Create an AWS SigV4 signer and sign the request
        var awsSigner = new AWS4Signer();
        awsSigner.Sign(
            request: marshalledRequest,
            clientConfig: _stsClient.Config,
            metrics: null,
            identity: AWSCredentials);

        // Create a signature object with the region and signed headers
        var signature = new CallerIdentitySignature
        {
            Region = _stsClient.Config.RegionEndpoint.SystemName,
            Headers = marshalledRequest.Headers
        };

        // Request an access token using the principal ID, signature, and scope
        var accessToken = await _accessTokenClient.GetAccessToken(
            _principalId,
            signature,
            scope);

        return accessToken;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates AWS credentials with automatic refresh.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <returns>AWS credentials that can be used for authentication.</returns>
    /// <remarks>
    /// Handles different types of credentials, wrapping <see cref="RefreshingAWSCredentials"/> to ensure timely refresh.
    /// </remarks>
    private static AWSCredentials CreateAWSCredentials(
        ILogger logger)
    {
        // Get the default AWS credentials
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // For refreshing credentials, wrap them to ensure timely refresh
        if (awsCredentials is RefreshingAWSCredentials refreshingAWSCredentials)
        {
            return RefreshingCredentials.Create(logger, refreshingAWSCredentials);
        }

        // For other credential types, initialize them and return
        _ = awsCredentials.GetCredentials();

        return awsCredentials;
    }

    #endregion

    #region RefreshingCredentials

    /// <summary>
    /// Wrapper for <see cref="RefreshingAWSCredentials"/> that ensures proactive credential refresh.
    /// </summary>
    /// <remarks>
    /// Addresses an issue in the AWS SDK where <see cref="RefreshingAWSCredentials"/> doesn't properly refresh credentials.
    /// </remarks>
    private class RefreshingCredentials : AWSCredentials
    {
        #region Private Static Fields

        /// <summary>
        /// Reflection access to the internal state of <see cref="RefreshingAWSCredentials"/>.
        /// </summary>
        private static readonly FieldInfo _currentStateField = typeof(RefreshingAWSCredentials)
            .GetField(
                "currentState",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

        /// <summary>
        /// Reflection access to the expiration time of credentials.
        /// </summary>
        private static readonly PropertyInfo _expirationProperty = typeof(RefreshingAWSCredentials.CredentialsRefreshState)
            .GetProperty(
                "Expiration",
                BindingFlags.Public | BindingFlags.Instance)!;

        #endregion

        #region Private Fields

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The underlying AWS credentials.
        /// </summary>
        private readonly RefreshingAWSCredentials _refreshingAWSCredentials;

        /// <summary>
        /// The timer used to schedule credential refresh.
        /// </summary>
        private readonly Timer _refreshTimer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshingCredentials"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="refreshingAWSCredentials">The underlying AWS credentials to wrap.</param>
        private RefreshingCredentials(
            ILogger logger,
            RefreshingAWSCredentials refreshingAWSCredentials)
        {
            _logger = logger;
            _refreshingAWSCredentials = refreshingAWSCredentials;

            // Create a timer but don't start it yet
            _refreshTimer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Creates a new instance of <see cref="RefreshingCredentials"/> and performs initial refresh.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="refreshingAWSCredentials">The underlying AWS credentials to wrap.</param>
        /// <returns>A new credentials instance with refreshed credentials.</returns>
        public static AWSCredentials Create(
            ILogger logger,
            RefreshingAWSCredentials refreshingAWSCredentials)
        {
            // Create the credentials wrapper
            var credentials = new RefreshingCredentials(logger, refreshingAWSCredentials);

            // Perform initial refresh
            credentials.Refresh(null);

            return credentials;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override ImmutableCredentials GetCredentials()
        {
            // Delegate to the underlying credentials
            return _refreshingAWSCredentials.GetCredentials();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates when the credentials should be refreshed.
        /// </summary>
        /// <returns>The UTC time when credentials should be refreshed.</returns>
        /// <remarks>
        /// Refresh time is the credential expiration time minus the preempt expiry time, but not less than 5 seconds from now.
        /// </remarks>
        private DateTime GetRefreshOn()
        {
            // Set minimum refresh time to not less than 5 seconds from now
            var minRefreshOn = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            // Get the current credential state using reflection
            var currentState = _currentStateField.GetValue(_refreshingAWSCredentials)!;
            if (currentState is null)
            {
                return minRefreshOn;
            }

            // Get the expiration time of the current credentials
            var expiration = (DateTime)_expirationProperty.GetValue(currentState)!;

            // Calculate refresh time as expiration minus preempt time
            var refreshOn = expiration - _refreshingAWSCredentials.PreemptExpiryTime;

            // Ensure refresh time is not too soon
            return refreshOn >= minRefreshOn
                ? refreshOn
                : minRefreshOn;
        }

        /// <summary>
        /// Refreshes the credentials and schedules the next refresh.
        /// </summary>
        /// <param name="state">The state object passed by the Timer (not used).</param>
        private void Refresh(object? state)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Log the refresh attempt.
            _logger.LogInformation(
                "AWSCredentialsManager.Refresh");

            // Force a refresh by setting expiration to minimum value
            var currentState = _currentStateField.GetValue(_refreshingAWSCredentials);
            if (currentState is not null)
            {
                _expirationProperty.SetValue(currentState, DateTime.MinValue);
            }

            // Trigger a refresh by calling GetCredentials
            _ = _refreshingAWSCredentials.GetCredentials();

            // Calculate the next refresh time
            var refreshOn = GetRefreshOn();

            // Log the scheduled refresh time
            _logger.LogInformation(
                "AWSCredentialsManager.Refresh: refreshOn = '{refreshOn:o}'.",
                refreshOn);

            // Calculate the time until the next refresh
            var dueTime = refreshOn - DateTime.UtcNow;

            // Schedule the next refresh
            _refreshTimer.Change(
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan);

            stopwatch.Stop();
            _logger.LogInformation(
                "AWSCredentialsManager.Refresh: elapsedMilliseconds = {elapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);
        }

        #endregion
    }

    #endregion
}
