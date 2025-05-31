namespace Trelnex.Core.Identity;

/// <summary>
/// Represents a security credential capable of acquiring access tokens.
/// </summary>
/// <remarks>
/// Defines the contract for credential implementations.
/// </remarks>
public interface ICredential
{
    /// <summary>
    /// Acquires an access token for the specified scope or resource.
    /// </summary>
    /// <param name="scope">The scope or resource identifier for which to obtain an access token.</param>
    /// <returns>An access token that can be used to authenticate requests to the specified scope.</returns>
    /// <exception cref="AccessTokenUnavailableException">
    /// Thrown when the access token cannot be acquired.
    /// </exception>
    AccessToken GetAccessToken(
        string scope);
}
