using System.Text.Json.Serialization;

namespace Trelnex.Core.Identity;

public class AccessToken
{
    /// <summary>
    /// Get the access token value.
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    /// <summary>
    /// Identifies the type of access token.
    /// </summary>
    [JsonPropertyName("tokenType")]
    public required string TokenType { get; init; }

    /// <summary>
    /// Gets the time when the provided token expires.
    /// </summary>
    [JsonPropertyName("expiresOn")]
    public DateTimeOffset ExpiresOn { get; init; }

    /// <summary>
    /// Gets the time when the token should be refreshed.
    /// </summary>
    [JsonPropertyName("refreshOn")]
    public DateTimeOffset? RefreshOn { get; init; }

    /// <summary>
    /// Gets the authorization header for this access token.
    /// </summary>
    /// <returns>The authorization header for this access token.</returns>
    public string GetAuthorizationHeader() => $"{TokenType} {Token}";
}
