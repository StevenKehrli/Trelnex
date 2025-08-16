using Azure.Core;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Configuration settings for Azure Cosmos DB client connection and container access.
/// </summary>
/// <param name="TokenCredential">Azure credential for authenticating Cosmos DB requests.</param>
/// <param name="AccountEndpoint">URI endpoint for the Cosmos DB account.</param>
/// <param name="DatabaseId">Cosmos DB database identifier.</param>
/// <param name="ContainerIds">Array of Cosmos DB container identifiers managed by this client.</param>
internal record CosmosClientOptions(
    TokenCredential TokenCredential,
    string AccountEndpoint,
    string DatabaseId,
    string[] ContainerIds);
