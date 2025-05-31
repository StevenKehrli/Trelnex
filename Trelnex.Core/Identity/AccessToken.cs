using System.Text.Json.Serialization;

namespace Trelnex.Core.Identity;

/// <summary>
/// Represents an OAuth 2.0 access token with associated metadata.
/// </summary>
public class AccessToken
{
    #region Public Properties

    /// <summary>
    /// Gets the actual access token value used for authentication.
    /// </summary>
    [JsonPropertyName("token")]
    public required string Token { get; init; }

    /// <summary>
    /// Gets the type of access token, which indicates the authentication scheme.
    /// </summary>
    [JsonPropertyName("tokenType")]
    public required string TokenType { get; init; }

    /// <summary>
    /// Gets the timestamp when this token will expire.
    /// </summary>
    [JsonPropertyName("expiresOn")]
    public DateTimeOffset ExpiresOn { get; init; }

    /// <summary>
    /// Gets the timestamp when this token should be proactively refreshed.
    /// </summary>
    [JsonPropertyName("refreshOn")]
    public DateTimeOffset? RefreshOn { get; init; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a formatted authorization header value using this token.
    /// </summary>
    /// <returns>A string formatted as "{TokenType} {Token}" suitable for use in HTTP Authorization headers.</returns>
    public string GetAuthorizationHeader() => $"{TokenType} {Token}";

    #endregion
}
