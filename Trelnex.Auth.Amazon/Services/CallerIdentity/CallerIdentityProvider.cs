using System.Collections.Concurrent;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Identity;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.Runtime.Internal.Util;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Trelnex.Core;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.CallerIdentity;

/// <summary>
/// Provides an interface for verifying caller identity using AWS IAM credentials.
/// </summary>
/// <remarks>
/// This interface is used to validate request signatures against AWS IAM,
/// allowing authentication of callers based on their AWS identity without
/// exchanging or persisting sensitive access keys.
/// </remarks>
public interface ICallerIdentityProvider
{
    /// <summary>
    /// Gets the caller identity from pre-signed AWS STS GetCallerIdentity request headers.
    /// </summary>
    /// <param name="region">The AWS region name from the caller for the security token service.</param>
    /// <param name="headers">The pre-signed request headers from the caller that will be used to validate their identity with AWS STS.</param>
    /// <returns>The caller identity ARN (principal ID) if authentication succeeds.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when the AWS STS service returns an error or when the identity cannot be verified.</exception>
    Task<string> GetAsync(
        string region,
        IDictionary<string, string> headers);
}

/// <summary>
/// Implements AWS IAM-based caller identity verification using pre-signed request headers.
/// </summary>
/// <remarks>
/// This implementation verifies caller identity by using the AWS STS GetCallerIdentity operation
/// with pre-signed request headers. This allows the system to authenticate callers without
/// requiring them to provide their AWS access keys directly.
///
/// The pre-signed headers contain all the authentication information necessary for AWS to verify
/// the caller's identity, and the verification succeeds only if the caller has valid AWS credentials.
/// </remarks>
internal class CallerIdentityProvider : ICallerIdentityProvider
{
    #region Private Fields

    /// <summary>
    /// The AWS credentials used by this service to make calls to AWS STS.
    /// </summary>
    private readonly AWSCredentials _credentials;

    /// <summary>
    /// A thread-safe collection of AWS STS clients mapped by region name.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Lazy{T}"/> initialization to ensure thread safety when creating clients,
    /// following the pattern described at:
    /// https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
    /// </remarks>
    private readonly ConcurrentDictionary<string, Lazy<AmazonSecurityTokenServiceClientOverride>> _clientsByRegion = new();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CallerIdentityProvider"/> class.
    /// </summary>
    /// <param name="credentials">The AWS credentials to use for making AWS STS calls.</param>
    private CallerIdentityProvider(
        AWSCredentials credentials)
    {
        // Set the AWS credentials.
        _credentials = credentials;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="CallerIdentityProvider"/> class.
    /// </summary>
    /// <param name="credentialProvider">The credential provider to get the AWS credentials.</param>
    /// <returns>A new <see cref="CallerIdentityProvider"/> instance.</returns>
    /// <remarks>
    /// This factory method allows dependency injection of credentials while
    /// ensuring the class itself has proper encapsulation.
    /// </remarks>
    public static CallerIdentityProvider Create(
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Get the AWS credentials from the provider.
        var credentials = credentialProvider.GetCredential();

        // Return a new CallerIdentityProvider instance.
        return new CallerIdentityProvider(credentials);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the caller identity from pre-signed AWS STS GetCallerIdentity request headers.
    /// </summary>
    /// <param name="region">The AWS region name where the identity verification should occur.</param>
    /// <param name="headers">The pre-signed request headers containing AWS authorization information.</param>
    /// <returns>The caller identity ARN (principal ID) if authentication succeeds.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown with ServiceUnavailable status when the AWS STS service returns an error.</exception>
    /// <remarks>
    /// This method verifies the caller's identity by:
    /// 1. Getting an STS client for the specified region
    /// 2. Creating a request with the pre-signed headers
    /// 3. Calling AWS STS GetCallerIdentity operation
    ///
    /// The AWS STS service will verify the signature in the headers and return
    /// the identity of the caller if the signature is valid.
    /// </remarks>
    public async Task<string> GetAsync(
        string region,
        IDictionary<string, string> headers)
    {
        // Get or create an STS client for the specified region.
        var client = GetClient(region);

        // Create a GetCallerIdentity request that will use the provided headers.
        var request = new GetCallerIdentityRequestOverride(headers);

        try
        {
            // Call AWS STS to verify identity - this only succeeds if the headers contain a valid signature.
            var response = await client.GetCallerIdentityAsync(request);

            // Return the verified AWS IAM ARN as the principal ID.
            return response.Arn;
        }
        catch (AmazonSecurityTokenServiceException ex)
        {
            // Wrap AWS exceptions with our standard HTTP status code exception.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets or creates an STS client for the specified AWS region.
    /// </summary>
    /// <param name="region">The AWS region name for the STS client.</param>
    /// <returns>A custom STS client that will use the pre-signed headers for authentication.</returns>
    /// <remarks>
    /// Uses a thread-safe pattern to ensure that only one client is created per region,
    /// lazily initializing clients as needed.
    /// </remarks>
    private AmazonSecurityTokenServiceClientOverride GetClient(
        string region)
    {
        // Get or create a lazily-initialized client for this region.
        var lazyClient =
            _clientsByRegion.GetOrAdd(
                key: region,
                value: new Lazy<AmazonSecurityTokenServiceClientOverride>(() =>
                {
                    // Convert the region name to a RegionEndpoint object.
                    var regionEndpoint = RegionEndpoint.GetBySystemName(region);

                    // Create a configuration for the STS client.
                    var config = new AmazonSecurityTokenServiceConfig
                    {
                        RegionEndpoint = regionEndpoint
                    };

                    // Create and return a new custom STS client.
                    return new AmazonSecurityTokenServiceClientOverride(_credentials, config);
                }));

        // Return the initialized client.
        return lazyClient.Value;
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// A custom AmazonSecurityTokenServiceClient that uses a custom signer.
    /// </summary>
    /// <remarks>
    /// This override allows us to inject our custom signing process into the AWS SDK pipeline,
    /// replacing the standard AWS credential-based signing with our approach of using
    /// pre-signed headers.
    /// </remarks>
    private class AmazonSecurityTokenServiceClientOverride(
        AWSCredentials credentials,
        AmazonSecurityTokenServiceConfig config)
        : AmazonSecurityTokenServiceClient(credentials, config)
    {
        /// <summary>
        /// Customizes the request processing pipeline to use our custom signer.
        /// </summary>
        /// <param name="pipeline">The runtime pipeline to customize.</param>
        protected override void CustomizeRuntimePipeline(
            RuntimePipeline pipeline)
        {
            // Set up the default pipeline.
            base.CustomizeRuntimePipeline(pipeline);

            // Replace the standard AWS4Signer with our custom implementation.
            var signer = new AWS4SignerOverride();
            var signerOverride = new SignerOverride(signer);
            pipeline.ReplaceHandler<Signer>(signerOverride);
        }
    }

    /// <summary>
    /// A Signer override that replaces the standard AWS signer in the request pipeline.
    /// </summary>
    /// <remarks>
    /// This class intercepts the signing process and replaces the default signer with our custom implementation.
    /// </remarks>
    private class SignerOverride(
        ISigner signer) : Signer
    {
        /// <summary>
        /// Overrides the synchronous invocation to replace the signer.
        /// </summary>
        /// <param name="executionContext">The execution context containing the request.</param>
        public override void InvokeSync(
            IExecutionContext executionContext)
        {
            // Replace the signer with our custom signer.
            executionContext.RequestContext.Signer = signer;

            // Continue the pipeline.
            base.InvokeSync(executionContext);
        }

        /// <summary>
        /// Overrides the asynchronous invocation to replace the signer.
        /// </summary>
        /// <typeparam name="T">The return type of the async operation.</typeparam>
        /// <param name="executionContext">The execution context containing the request.</param>
        /// <returns>The result of the async operation.</returns>
        public override async Task<T> InvokeAsync<T>(
            IExecutionContext executionContext)
        {
            // Replace the signer with our custom signer.
            executionContext.RequestContext.Signer = signer;

            // Continue the pipeline.
            return await base.InvokeAsync<T>(executionContext);
        }
    }

    /// <summary>
    /// A custom AWS4Signer that uses pre-provided headers instead of calculating signatures.
    /// </summary>
    /// <remarks>
    /// This class skips the standard AWS signature generation process and instead
    /// applies the pre-signed headers directly to the request.
    /// </remarks>
    private class AWS4SignerOverride : AWS4Signer
    {
        /// <summary>
        /// Overrides the standard AWS4 signing process to apply pre-signed headers.
        /// </summary>
        /// <param name="request">The request to sign.</param>
        /// <param name="clientConfig">The AWS client configuration.</param>
        /// <param name="metrics">The request metrics.</param>
        /// <param name="identity">The base identity for signing.</param>
        public override void Sign(
            IRequest request,
            IClientConfig clientConfig,
            RequestMetrics metrics,
            BaseIdentity identity)
        {
            // Cast the original request to our custom type to access the headers.
            var requestOverride = (request.OriginalRequest as GetCallerIdentityRequestOverride)!;

            // Apply each pre-signed header to the request.
            foreach (var header in requestOverride.Headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            // No need to call base.Sign() as we're completely replacing the signing process.
        }
    }

    /// <summary>
    /// A custom GetCallerIdentity request that carries pre-signed headers.
    /// </summary>
    /// <remarks>
    /// This class extends the standard GetCallerIdentity request to include
    /// pre-signed headers that will be used for authentication.
    /// </remarks>
    private class GetCallerIdentityRequestOverride(
        IDictionary<string, string> headers)
        : GetCallerIdentityRequest
    {
        /// <summary>
        /// Gets the pre-signed headers to use for authentication.
        /// </summary>
        public IDictionary<string, string> Headers => headers;
    }

    #endregion
}
