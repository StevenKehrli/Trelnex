namespace Trelnex.Core.Identity;

public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets the name of the credential.
    /// </summary>
    string CredentialName { get; }

    /// <summary>
    /// Gets the scope of the access token
    /// </summary>
    string Scope { get; }

    /// <summary>
    /// Gets the authorization header.
    /// </summary>
    /// <returns>The authorization header.</returns>
    string GetAuthorizationHeader();

    /// <summary>
    /// Gets the <see cref="IAccessToken"/>.
    /// </summary>
    /// <returns>The <see cref="IAccessToken"/>.</returns>
    IAccessToken GetToken();
}
