namespace Trelnex.Core.Identity;

/// <summary>
/// Represents the overall status of a credential, including all associated access tokens.
/// </summary>
/// <param name="Statuses">Collection of status information for each access token associated with this credential.</param>
public record CredentialStatus(
    AccessTokenStatus[] Statuses);
