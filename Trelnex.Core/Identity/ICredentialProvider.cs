namespace Trelnex.Core.Identity;

public interface ICredentialProvider
{
    /// <summary>
    /// Gets the array of <see cref="ICredentialStatusProvider"/> for all credentials.
    /// </summary>
    /// <returns>The array of <see cref="ICredentialStatusProvider"/>.</returns>
    ICredentialStatusProvider[] GetStatusProviders();

    /// <summary>
    /// Gets the <see cref="IAccessTokenProvider"/> for the specified credential name and scope.
    /// </summary>
    /// <param name="credentialName">The name of the credential.</param>
    /// <param name="scope">The scope of the token.</param>
    /// <returns>The <see cref="IAccessTokenProvider"/> for the specified credential name.</returns>
    IAccessTokenProvider GetTokenProvider(
        string credentialName,
        string scope);
}
