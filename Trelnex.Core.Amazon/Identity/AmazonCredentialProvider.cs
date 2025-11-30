using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Provides AWS credentials and Trelnex access tokens for Amazon-based authentication.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ICredentialProvider{T}"/> for <see cref="AWSCredentials"/>,
/// serving as the entry point for AWS credential and Trelnex token management.
/// </para>
/// <para>
/// Wraps a <see cref="ManagedCredential"/> instance that handles AWS credential refresh
/// and Trelnex access token acquisition via AWS SigV4 signed requests.
/// </para>
/// </remarks>
internal class AmazonCredentialProvider : ICredentialProvider<AWSCredentials>
{
    #region Private Fields

    /// <summary>
    /// The managed credential that provides both AWS credentials and Trelnex access tokens.
    /// </summary>
    private readonly ManagedCredential _managedCredential;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonCredentialProvider"/> class.
    /// </summary>
    /// <param name="managedCredential">The managed credential instance.</param>
    private AmazonCredentialProvider(
        ManagedCredential managedCredential)
    {
        _managedCredential = managedCredential;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="AmazonCredentialProvider"/> instance with initialized credentials.
    /// </summary>
    /// <param name="options">Configuration options for AWS region and token client endpoint.</param>
    /// <param name="logger">The logger for recording operations.</param>
    /// <returns>A fully initialized <see cref="AmazonCredentialProvider"/>.</returns>
    /// <remarks>
    /// <para>
    /// Creates AWS credentials with proactive refresh using <see cref="AWSCredentialsManager.CreateAWSCredentials"/>,
    /// then initializes a <see cref="ManagedCredential"/> with STS client and access token client.
    /// </para>
    /// <para>
    /// This is the primary entry point for creating an Amazon credential provider.
    /// </para>
    /// </remarks>
    public static async Task<AmazonCredentialProvider> CreateAsync(
        AmazonCredentialOptions options,
        ILogger logger)
    {
        // Create AWS credentials with automatic proactive refresh
        var awsCredentials = AWSCredentialsManager.CreateAWSCredentials(logger);

        // Initialize the managed credential with STS and token client
        var managedCredential = await ManagedCredential.Create(awsCredentials, options, logger);

        // Wrap in the provider
        return new AmazonCredentialProvider(managedCredential);
    }

    #endregion

    #region ICredentialProvider

    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    public string Name => "Amazon";

    /// <summary>
    /// Creates an access token provider for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the access token.</param>
    /// <returns>An <see cref="IAccessTokenProvider"/> for the specified scope.</returns>
    /// <remarks>
    /// <para>
    /// Creates an <see cref="AccessTokenProvider"/> that wraps the managed credential's
    /// token acquisition functionality for the specified scope.
    /// </para>
    /// <para>
    /// Each scope gets its own cached token with automatic refresh in the underlying
    /// <see cref="ManagedCredential"/>.
    /// </para>
    /// </remarks>
    public IAccessTokenProvider GetAccessTokenProvider(
        string scope)
    {
        // Create a provider that wraps the managed credential for this scope
        return AccessTokenProvider.Create(_managedCredential, scope);
    }

    /// <summary>
    /// Gets the health status of all managed access tokens.
    /// </summary>
    /// <returns>A <see cref="CredentialStatus"/> containing status information for all scopes.</returns>
    /// <remarks>
    /// Delegates to the underlying <see cref="ManagedCredential"/> to collect status
    /// information from all cached token items.
    /// </remarks>
    public CredentialStatus GetStatus()
    {
        // Retrieve status from the managed credential
        return _managedCredential.GetStatus();
    }

    #endregion

    #region ICredentialProvider<AWSCredentials>

    /// <summary>
    /// Gets the AWS credentials for use with AWS SDK clients.
    /// </summary>
    /// <returns>The <see cref="AWSCredentials"/> instance.</returns>
    /// <remarks>
    /// <para>
    /// Returns the <see cref="ManagedCredential"/> which implements <see cref="AWSCredentials"/>
    /// and can be passed directly to AWS SDK client constructors.
    /// </para>
    /// <para>
    /// The returned credentials include automatic proactive refresh before expiration.
    /// </para>
    /// </remarks>
    public AWSCredentials GetCredential()
    {
        // Return the managed credential as AWSCredentials
        return _managedCredential;
    }

    #endregion
}
