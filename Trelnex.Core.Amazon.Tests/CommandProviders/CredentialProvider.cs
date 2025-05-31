using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

/// <summary>
/// Custom implementation of ICredentialProvider for providing AWS credentials in tests.
/// </summary>
/// <remarks>
/// This class provides a simple implementation of the ICredentialProvider interface
/// for use in tests. It uses the default AWS credentials identity resolver to obtain
/// credentials, and implements only the essential methods needed for these tests.
/// </remarks>
internal class CredentialProvider : ICredentialProvider<AWSCredentials>
{
    private static readonly AWSCredentials _credentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    public string Name => "Amazon";

    /// <summary>
    /// Gets an access token provider for the specified scope.
    /// </summary>
    /// <param name="scope">The scope for which to get an access token provider.</param>
    /// <returns>An IAccessTokenProvider instance.</returns>
    /// <exception cref="NotImplementedException">This method is not implemented for tests.</exception>
    public IAccessTokenProvider GetAccessTokenProvider(
        string scope)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the AWS credentials.
    /// </summary>
    /// <returns>The AWS credentials.</returns>
    public AWSCredentials GetCredential()
    {
        return _credentials;
    }

    /// <summary>
    /// Gets the status of the credential provider.
    /// </summary>
    /// <returns>The credential status.</returns>
    /// <exception cref="NotImplementedException">This method is not implemented for tests.</exception>
    public CredentialStatus GetStatus()
    {
        throw new NotImplementedException();
    }
}
