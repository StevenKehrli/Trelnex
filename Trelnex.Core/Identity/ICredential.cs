namespace Trelnex.Core.Identity;

/// <summary>
/// Represents a credential.
/// </summary>
public interface ICredential
{
    /// <summary>
    /// Gets the <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.
    /// </summary>
    /// <param name="scope">The scope required for the token.</param>
    /// <returns>The <see cref="Trelnex.Core.Identity.AccessToken"/> for the specified scope.</returns>
    AccessToken GetAccessToken(
        string scope);
}
