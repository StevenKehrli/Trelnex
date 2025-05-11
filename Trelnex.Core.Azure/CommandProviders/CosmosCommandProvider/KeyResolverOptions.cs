using Azure.Core;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Configuration options for the Azure Key Vault key resolver.
/// </summary>
/// <param name="TokenCredential">The credential used to authenticate Key Vault requests.</param>
/// <remarks>For Cosmos DB client-side encryption to resolve keys from Azure Key Vault.</remarks>
internal record KeyResolverOptions(
    TokenCredential TokenCredential);
