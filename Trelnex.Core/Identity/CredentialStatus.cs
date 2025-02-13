namespace Trelnex.Core.Identity;

/// <summary>
/// Represents the status of a credential.
/// </summary>
/// <param name="Statuses">The statuses of the access tokens for the credential.</param>
public record CredentialStatus(
    AccessTokenStatus[] Statuses);
