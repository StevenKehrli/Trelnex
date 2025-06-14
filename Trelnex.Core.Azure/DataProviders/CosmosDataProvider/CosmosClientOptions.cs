using Azure.Core;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Configuration options for connecting to Azure Cosmos DB.
/// </summary>
/// <param name="TokenCredential">The credential used to authenticate Cosmos DB requests.</param>
/// <param name="AccountEndpoint">The URI to the Cosmos DB account.</param>
/// <param name="DatabaseId">The ID of the Cosmos DB database.</param>
/// <param name="ContainerIds">The collection of container IDs in the database.</param>
/// <remarks>Parameters to connect to Cosmos DB using token-based authentication.</remarks>
internal record CosmosClientOptions(
    TokenCredential TokenCredential,
    string AccountEndpoint,
    string DatabaseId,
    string[] ContainerIds);
