namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// Configuration options for creating an <see cref="AzureCredentialProvider"/> instance.
/// </summary>
/// <remarks>
/// Configures the credential sources for Azure authentication.
/// Loaded from appsettings.json under the "AzureCredentials" section.
/// </remarks>
/// <param name="Sources">The array of <see cref="CredentialSource"/> values to specify the credential sources when creating the <see cref="ChainedTokenCredential"/> instance.</param>
internal record AzureCredentialOptions(
    CredentialSource[] Sources);
