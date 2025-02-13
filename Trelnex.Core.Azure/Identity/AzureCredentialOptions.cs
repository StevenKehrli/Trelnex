namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// Represents the configuration options for creating an <see cref="AzureCredentialProvider"/> instance.
/// </summary>
/// <param name="Sources">The array of <see cref="CredentialSource"/> values to specify the credential sources when creating the <see cref="AzureCredential"/> instance.</param>
internal record AzureCredentialOptions(
    CredentialSource[] Sources);
