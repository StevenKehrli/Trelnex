using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// A credential provider that manages AWS credentials and access tokens.
/// </summary>
/// <remarks>
/// Implements <see cref="ICredentialProvider{T}"/> for <see cref="AWSCredentials"/>.
/// Creates a <see cref="ManagedCredential"/> that combines AWS credentials with token management.
/// </remarks>
internal class AmazonCredentialProvider : ICredentialProvider<AWSCredentials>
{
    #region Private Fields

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The managed credential.
    /// </summary>
    private readonly ManagedCredential _managedCredential;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonCredentialProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="awsCredentialsManager">The credentials manager.</param>
    private AmazonCredentialProvider(
        ILogger logger,
        AWSCredentialsManager awsCredentialsManager)
    {
        _logger = logger;
        _managedCredential = new ManagedCredential(logger, awsCredentialsManager);
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="AmazonCredentialProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options that configure the AWS credentials and token client.</param>
    /// <returns>A new instance of the <see cref="AmazonCredentialProvider"/>.</returns>
    /// <remarks>
    /// Initializes an <see cref="AWSCredentialsManager"/> and creates the provider.
    /// </remarks>
    public static async Task<AmazonCredentialProvider> Create(
        ILogger logger,
        AmazonCredentialOptions options)
    {
        // Create the AWS credentials manager
        var awsCredentialsManager = await AWSCredentialsManager.Create(logger, options);

        // Create and return the credential provider
        return new AmazonCredentialProvider(logger, awsCredentialsManager);
    }

    #endregion

    #region ICredentialProvider

    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    public string Name => "Amazon";

    /// <summary>
    /// Creates an <see cref="IAccessTokenProvider"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope of the token to request.</param>
    /// <returns>An <see cref="IAccessTokenProvider"/>.</returns>
    /// <remarks>
    /// Uses the underlying <see cref="ManagedCredential"/> to obtain tokens.
    /// </remarks>
    public IAccessTokenProvider GetAccessTokenProvider(
        string scope)
    {
        // Create and return a new AccessTokenProvider that uses the ManagedCredential
        return AccessTokenProvider.Create(_managedCredential, scope);
    }

    /// <summary>
    /// Gets the current status of the credential.
    /// </summary>
    /// <returns>A <see cref="CredentialStatus"/> object.</returns>
    /// <remarks>
    /// Delegates to <see cref="ManagedCredential.GetStatus"/>.
    /// </remarks>
    public CredentialStatus GetStatus()
    {
        // Delegate to the ManagedCredential to get the status
        return _managedCredential.GetStatus();
    }

    #endregion

    #region ICredentialProvider<AWSCredentials>

    /// <summary>
    /// Gets the underlying <see cref="AWSCredentials"/>.
    /// </summary>
    /// <returns>The <see cref="AWSCredentials"/> instance.</returns>
    /// <remarks>
    /// Provides direct access to the underlying <see cref="AWSCredentials"/> for AWS SDK clients.
    /// </remarks>
    public AWSCredentials GetCredential()
    {
        // Return the ManagedCredential, which implements AWSCredentials
        return _managedCredential;
    }

    #endregion
}
