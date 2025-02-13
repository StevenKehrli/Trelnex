namespace Trelnex.Core.Client;

/// <summary>
/// Represents the configuration properties for a client.
/// </summary>
/// <param name="CredentialProviderName">The name of the <see cref="ICredentialProvider"/> to get the <see cref="AccessToken"/>.</param>
/// <param name="Scope">The required scope for the <see cref="AccessToken"/>.</param>
/// <param name="BaseAddress">The base address <see cref="Uri"/> to build the request <see cref="Uri"/>.</param>
internal record ClientConfiguration(
    string CredentialProviderName,
    string Scope,
    Uri BaseAddress);
