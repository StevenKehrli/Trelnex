namespace Trelnex.Core.Identity;

/// <summary>
/// Represents detailed status information about an access token.
/// </summary>
/// <param name="Health">The current health status of the access token.</param>
/// <param name="Scopes">The permission scopes granted to the access token.</param>
/// <param name="ExpiresOn">The timestamp when the token will expire, if known.</param>
/// <param name="Data">Optional provider-specific or contextual information about the token.</param>
public record AccessTokenStatus(
    AccessTokenHealth Health,
    string[] Scopes,
    DateTimeOffset? ExpiresOn,
    IReadOnlyDictionary<string, object?>? Data = null);
