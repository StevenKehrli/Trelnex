namespace Trelnex.Core.Identity;

public interface IAccessToken
{
    /// <summary>
    /// Get the access token value.
    /// </summary>
    string Token { get; }

    /// <summary>
    /// Identifies the type of access token.
    /// </summary>
    string TokenType { get; }

    /// <summary>
    /// Gets the time when the provided token expires.
    /// </summary>
    DateTimeOffset ExpiresOn { get; }

    /// <summary>
    /// Gets the time when the token should be refreshed.
    /// </summary>
    DateTimeOffset? RefreshOn { get; }

    /// <summary>
    /// Gets the authorization header for this access token.
    /// </summary>
    /// <returns>The authorization header for this access token.</returns>
    string GetAuthorizationHeader();
}
