namespace Trelnex.Core.Identity;

/// <summary>
/// Represents the status of an access token.
/// </summary>
/// <param name="Health">A value describing the health of the access token. See <see cref="AccessTokenHealth"/>.</param>
/// <param name="Scopes">The scopes of the access token.</param>
/// <param name="ExpiresOn">The time when the access token expires.</param>
/// <param name="Data">Additional key-value pairs describing the status of the access token.</param>
public record AccessTokenStatus(
    AccessTokenHealth Health,
    string[] Scopes,
    DateTimeOffset? ExpiresOn,
    IReadOnlyDictionary<string, object?>? Data = null);
