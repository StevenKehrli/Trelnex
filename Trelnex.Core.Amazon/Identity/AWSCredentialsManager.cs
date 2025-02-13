using System.Reflection;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Auth;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Amazon.SecurityToken.Model.Internal.MarshallTransformations;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// A class to manage the <see cref="AWSCredentials"/> and get the <see cref="Trelnex.Core.Identity.AccessToken"/> from the Amazon /oauth2/token endpoint.
/// </summary>
public class AWSCredentialsManager
{
    /// <summary>
    /// Gets the Credentials property of the <see cref="AmazonSecurityTokenServiceClient"/>.
    /// </summary>
    private static readonly PropertyInfo _credentialsProperty =
            typeof(AmazonSecurityTokenServiceClient)
            .GetProperty(
                "Credentials",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// The <see cref="AccessTokenClient"/> to get the <see cref="Trelnex.Core.Identity.AccessToken"/> from the Amazon /oauth2/token endpoint.
    /// </summary>
    private readonly AccessTokenClient _accessTokenClient;

    /// <summary>
    /// The <see cref="AmazonSecurityTokenServiceClient"/> to get the caller identity.
    /// </summary>
    private readonly AmazonSecurityTokenServiceClient _stsClient;

    /// <summary>
    /// The current caller identity.
    /// </summary>
    private readonly string _principalId;

    private AWSCredentialsManager(
        AccessTokenClient accessTokenClient,
        AmazonSecurityTokenServiceClient stsClient,
        string principalId)
    {
        _accessTokenClient = accessTokenClient;
        _stsClient = stsClient;
        _principalId = principalId;
    }

    public AWSCredentials AWSCredentials => (AWSCredentials) _credentialsProperty.GetValue(_stsClient)!;

    /// <summary>
    /// Initializes a new instance of the <see cref="AWSCredentialsManager"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="options">The <see cref="AmazonCredentialOptions"/>.</param>
    /// <returns>The <see cref="AWSCredentialsManager"/>.</returns>
    public async static Task<AWSCredentialsManager> Create(
        ILogger logger,
        AmazonCredentialOptions options)
    {
        // create the AWSCredentials
        var credentials = CreateAWSCredentials(logger);

        // create the security token service client
        var regionEndpoint = RegionEndpoint.GetBySystemName(options.Region);

        var clientConfig = new AmazonSecurityTokenServiceConfig
        {
            RegionEndpoint = regionEndpoint,
            StsRegionalEndpoints = StsRegionalEndpointsValue.Regional
        };

        var stsClient = new AmazonSecurityTokenServiceClient(credentials, clientConfig);

        // get the caller identity
        var request = new GetCallerIdentityRequest();
        var response = await stsClient.GetCallerIdentityAsync(request);

        logger.LogInformation(
            "AWSCredentialsManager: principalId = '{Arn:l}', region = '{region:l}'",
            response.Arn,
            options.Region);

        // get the access token client
        HttpClient httpClient = new()
        {
            BaseAddress = options.AccessTokenClient.BaseAddress
        };

        var accessTokenClient = new AccessTokenClient(httpClient);

        // create the manager
        return new AWSCredentialsManager(
            accessTokenClient,
            stsClient,
            response.Arn);
    }

    /// <summary>
    /// Gets the <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>The <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.</returns>
    public async Task<AccessToken> GetAccessToken(
        string scope)
    {
        // marshal the request to the AWS IRequest
        var request = new GetCallerIdentityRequest();
        var marshaller = new GetCallerIdentityRequestMarshaller();
        var marshalledRequest = marshaller.Marshall(request);

        // get the endpoint for the sts client
        var endpoint = _stsClient.DetermineServiceOperationEndpoint(request);
        marshalledRequest.Endpoint = new Uri(endpoint.URL);

        // sign the request
        var signer = new AWS4Signer();

        signer.Sign(
            request: marshalledRequest,
            clientConfig: _stsClient.Config,
            metrics: null,
            credentials: AWSCredentials.GetCredentials());

        // create the signature
        var signature = new CallerIdentitySignature
        {
            Region = _stsClient.Config.RegionEndpoint.SystemName,
            Headers = marshalledRequest.Headers
        };

        // get a new token and set
        var accessToken = await _accessTokenClient.GetAccessToken(
            _principalId,
            signature,
            scope);

        return accessToken;
    }

    /// <summary>
    /// Creates the <see cref="AWSCredentials"/>.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <returns>The <see cref="AWSCredentials"/>.</returns>
    private static AWSCredentials CreateAWSCredentials(
        ILogger logger)
    {
        // create the AWSCredentials
        var credentials = FallbackCredentialsFactory.GetCredentials();

        // create the refreshing credentials if necessary
        if (credentials is RefreshingAWSCredentials refreshingAWSCredentials)
        {
            return RefreshingCredentials.Create(logger, refreshingAWSCredentials);
        }

        // initialize the credentials and return
        _ = credentials.GetCredentials();

        return credentials;
    }

    /// <summary>
    /// This class will refresh the underlying <see cref="RefreshingAWSCredentials"/> when the refreshOn time is reached (Expiration - PreemptExpiryTime).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="RefreshingCredentials"/> will refresh on-demand when the expiration time is reached.
    /// There is an existing bug that fails to refresh when the refreshOn time is reached.
    /// https://github.com/aws/aws-sdk-net/issues/3613
    /// </para>
    /// <para>
    /// This class will refresh the credentials using a timer, ensuring that valid credentials are available without waiting for the on-demand refresh.
    /// </para>
    /// </remarks>
    private class RefreshingCredentials : AWSCredentials
    {
        /// <summary>
        /// Gets the currentState field of the <see cref="RefreshingAWSCredentials"/>.
        /// </summary>
        private static readonly FieldInfo _currentStateField =
            typeof(RefreshingAWSCredentials)
            .GetField(
                "currentState",
                BindingFlags.NonPublic | BindingFlags.Instance)!;

        /// <summary>
        /// Gets the Expiration property of the <see cref="RefreshingAWSCredentials.CredentialsRefreshState"/>.
        /// </summary>
        private static readonly PropertyInfo _expirationProperty =
            typeof(RefreshingAWSCredentials.CredentialsRefreshState)
            .GetProperty(
                "Expiration",
                BindingFlags.Public | BindingFlags.Instance)!;

        /// <summary>
        /// The <see cref="ILogger"/>.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The underlying <see cref="RefreshingAWSCredentials"/>.
        /// </summary>
        private readonly RefreshingAWSCredentials _refreshingAWSCredentials;

        /// <summary>
        /// The <see cref="Timer"/> to refresh the <see cref="RefreshingAWSCredentials"/>.
        /// </summary>
        private readonly Timer _timer;

        private RefreshingCredentials(
            ILogger logger,
            RefreshingAWSCredentials refreshingAWSCredentials)
        {
            _logger = logger;
            _refreshingAWSCredentials = refreshingAWSCredentials;

            _timer = new Timer(Refresh, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="AWSCredentials"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="refreshingAWSCredentials">The underlying <see cref="RefreshingAWSCredentials"/>.</param>
        /// <returns>The <see cref="AWSCredentials"/>.</returns>
        public static AWSCredentials Create(
            ILogger logger,
            RefreshingAWSCredentials refreshingAWSCredentials)
        {
            var credentials = new RefreshingCredentials(logger, refreshingAWSCredentials);

            credentials.Refresh(null);

            return credentials;
        }

        public override ImmutableCredentials GetCredentials()
        {
            return _refreshingAWSCredentials.GetCredentials();
        }

        /// <summary>
        /// Get the refresh time of the <see cref="_refreshingAWSCredentials"/>.
        /// </summary>
        /// <returns>The <see cref="DateTime"/> of the next refresh.</returns>
        private DateTime GetRefreshOn()
        {
            // not less than 5 seconds from now
            var minRefreshOn = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            // get the current state
            var currentState = _currentStateField.GetValue(_refreshingAWSCredentials)!;
            if (currentState is null)
            {
                return minRefreshOn;
            }

            // get the expiration time
            var expiration = (DateTime) _expirationProperty.GetValue(currentState)!;

            // get the refresh time
            var refreshOn = expiration - _refreshingAWSCredentials.PreemptExpiryTime;

            return refreshOn >= minRefreshOn
                ? refreshOn
                : minRefreshOn;
        }

        /// <summary>
        /// The <see cref="TimerCallback"/> delegate of the <see cref="_timer"/> <see cref="Timer"/>.
        /// </summary>
        /// <param name="state">An object containing application-specific information relevant to the method invoked by this delegate, or null.</param>
        private void Refresh(object? state)
        {
            // set the expiration time to force a refresh
            var currentState = _currentStateField.GetValue(_refreshingAWSCredentials);
            if (currentState is not null)
            {
                _expirationProperty.SetValue(currentState, DateTime.MinValue);
            }

            // get the current credentials
            _ = _refreshingAWSCredentials.GetCredentials();

            // get the refresh time
            var refreshOn = GetRefreshOn();

            _logger.LogInformation(
                "AWSCredentialsManager.Refresh: refreshOn = '{refreshOn:o}'.",
                refreshOn);

            // get the due time
            var dueTime = refreshOn - DateTime.UtcNow;

            _timer.Change(
                dueTime: dueTime,
                period: Timeout.InfiniteTimeSpan);
        }
    }
}
