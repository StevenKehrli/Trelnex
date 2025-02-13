namespace Trelnex.Core.Identity;

public interface ICredentialProvider
{
    /// <summary>
    /// Gets the name of the credential provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the <see cref="IAccessTokenProvider"/> for the specified credential name and scope.
    /// </summary>
    /// <param name="scope">The scope of the token.</param>
    /// <returns>The <see cref="IAccessTokenProvider"/> for the specified credential name.</returns>
    IAccessTokenProvider<TClient> GetAccessTokenProvider<TClient>(
        string scope);

    /// <summary>
    /// Gets the <see cref="CredentialStatus"/> of the credential used by this token provider.
    /// </summary>
    /// <returns>The <see cref="CredentialStatus"/> of the credential used by this token provider.</returns>
    CredentialStatus GetStatus();
}

/// <summary>
/// Represents a provider to retrieve credentials of type <see cref="TCredential"/>.
/// </summary>
/// <typeparam name="TCredential">The type of credential (e.g., TokenCredential, AWSCredentials, GoogleCredential, etc.).</typeparam>
public interface ICredentialProvider<TCredential> : ICredentialProvider
{
    /// <summary>
    /// Gets the credential of type <see cref="TCredential"/> for the specified credential name.
    /// </summary>
    /// <returns>The credential of type <see cref="TCredential"/>.</returns>
    TCredential GetCredential();
}
