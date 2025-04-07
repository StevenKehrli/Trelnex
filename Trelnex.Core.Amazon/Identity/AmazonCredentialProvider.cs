using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// A class that provides AWS credentials and status.
/// </summary>
internal class AmazonCredentialProvider : ICredentialProvider<AWSCredentials>
{
    /// <summary>
    /// The <see cref="ILogger"/>.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// The <see cref="ManagedCredential"/> over the <see cref="AWSCredentials"/>.
    /// </summary>
    private readonly ManagedCredential _managedCredential;

    private AmazonCredentialProvider(
        ILogger logger,
        AWSCredentialsManager awsCredentialsManager)
    {
        _logger = logger;
        _managedCredential = new ManagedCredential(logger, awsCredentialsManager);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonCredentialProvider"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="options">The <see cref="AmazonCredentialOptions"/>.</param>
    /// <returns>The <see cref="AmazonCredentialProvider"/>.</returns>
    public async static Task<AmazonCredentialProvider> Create(
        ILogger logger,
        AmazonCredentialOptions options)
    {
        // create the AWSCredentials
        var awsCredentialsManager = await AWSCredentialsManager.Create(logger, options);

        // create the credential provider
        return new AmazonCredentialProvider(logger, awsCredentialsManager);
    }

#region ICredentialProvider

    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    public string Name => "Amazon";

    /// <summary>
    /// Gets the <see cref="IAccessTokenProvider"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope of the token.</param>
    /// <returns>The <see cref="IAccessTokenProvider"/> for the specified scope.</returns>
    public IAccessTokenProvider GetAccessTokenProvider(
        string scope)
    {
        return AccessTokenProvider.Create(_managedCredential, scope);
    }

    /// <summary>
    /// Gets the array of <see cref="ICredentialStatusProvider"/> for all credentials.
    /// </summary>
    /// <returns>The array of <see cref="ICredentialStatusProvider"/>.</returns>
    public CredentialStatus GetStatus()
    {
        return _managedCredential.GetStatus();
    }

#endregion ICredentialProvider

#region ICredentialProvider<TokenCredential>

    /// <summary>
    /// Gets the <see cref="AWSCredentials"/> for the specified credential name.
    /// </summary>
    /// <returns>The <see cref="AWSCredentials"/>.</returns>
    public AWSCredentials GetCredential()
    {
        return _managedCredential;
    }

#endregion ICredentialProvider<TokenCredential>

}
