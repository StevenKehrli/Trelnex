using System.Collections.Concurrent;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Util;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Trelnex.Core;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.CallerIdentity;

/// <summary>
/// Represents the caller identity provider.
/// </summary>
public interface ICallerIdentityProvider
{
    /// <summary>
    /// Gets the caller identity.
    /// </summary>
    /// <param name="region">The region name from the caller for the security token service.</param>
    /// <param name="headers">The headers from the caller authenticating the request to the security token service and therefore the caller identity.</param>
    /// <returns>The callery identity arn (principal id).</returns>
    Task<string> GetAsync(
        string region,
        IDictionary<string, string> headers);
}

internal class CallerIdentityProvider : ICallerIdentityProvider
{
    private readonly AWSCredentials _credentials;

    /// <summary>
    /// A thread-safe collection of <see cref="string"/> to <see cref="AmazonSecurityTokenServiceClientOverride"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, Lazy<AmazonSecurityTokenServiceClientOverride>> _clientsByRegion = new();

    private CallerIdentityProvider(
        AWSCredentials credentials)
    {
        _credentials = credentials;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CallerIdentityProvider"/>.
    /// </summary>
    /// <param name="credentialProvider">The credential provider to get the AWS credentials.</param>
    /// <returns>The <see cref="CallerIdentityProvider"/>.</returns>
    public static CallerIdentityProvider Create(
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        var credentials = credentialProvider.GetCredential();

        return new CallerIdentityProvider(credentials);
    }

    /// <summary>
    /// Gets the caller identity.
    /// </summary>
    /// <param name="region">The region name from the caller for the security token service.</param>
    /// <param name="headers">The headers from the caller authenticating the request to the security token service and therefore the caller identity.</param>
    /// <returns>The callery identity arn (principal id).</returns>
    public async Task<string> GetAsync(
        string region,
        IDictionary<string, string> headers)
    {
        // get the client
        var client = GetClient(region);

        // create the request
        var request = new GetCallerIdentityRequestOverride(headers);

        try
        {
            var response = await client.GetCallerIdentityAsync(request);

            return response.Arn;
        }
        catch (AmazonSecurityTokenServiceException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Gets the <see cref="AmazonSecurityTokenServiceClientOverride"/> for the specified region.
    /// </summary>
    /// <param name="region">The region name for the security token service client.</param>
    /// <returns>The <see cref="AmazonSecurityTokenServiceClientOverride"/>.</returns>
    private AmazonSecurityTokenServiceClientOverride GetClient(
        string region)
    {
        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyClient =
            _clientsByRegion.GetOrAdd(
                key: region,
                value: new Lazy<AmazonSecurityTokenServiceClientOverride>(() =>
                {
                    var regionEndpoint = RegionEndpoint.GetBySystemName(region);

                    var config = new AmazonSecurityTokenServiceConfig
                    {
                        RegionEndpoint = regionEndpoint,
                        StsRegionalEndpoints = StsRegionalEndpointsValue.Regional
                    };

                    return new AmazonSecurityTokenServiceClientOverride(_credentials, config);
                }));

        return lazyClient.Value;
    }

    /// <summary>
    /// A custom AmazonSecurityTokenServiceClient that uses a custom signer.
    /// </summary>
    /// <param name="credentials">AWS Credentials</param>
    /// <param name="config">The <see cref="AmazonSecurityTokenServiceConfig"/> to configure the client.</param>
    private class AmazonSecurityTokenServiceClientOverride(
        AWSCredentials credentials,
        AmazonSecurityTokenServiceConfig config)
        : AmazonSecurityTokenServiceClient(credentials, config)
    {
        private AbstractAWSSigner _signer = new AWS4SignerOverride();

        protected override AbstractAWSSigner CreateSigner()
        {
            return _signer;
        }
    }

    /// <summary>
    /// A custom AWS4Signer that signs the request with the provided headers.
    /// </summary>
    private class AWS4SignerOverride : AWS4Signer
    {
        public override ClientProtocol Protocol => base.Protocol;

        public override void Sign(
            IRequest request,
            IClientConfig clientConfig,
            RequestMetrics metrics,
            string awsAccessKeyId,
            string awsSecretAccessKey)
        {
            var requestOverride = (request.OriginalRequest as GetCallerIdentityRequestOverride)!;

            foreach (var header in requestOverride.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
    }

    private class GetCallerIdentityRequestOverride(
        IDictionary<string, string> headers)
        : GetCallerIdentityRequest
    {
        public IDictionary<string, string> Headers => headers;
    }
}
