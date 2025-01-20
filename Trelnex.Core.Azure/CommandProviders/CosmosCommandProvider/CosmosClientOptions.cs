using Azure.Core;

namespace Trelnex.Core.Data;

public record CosmosClientOptions(
    TokenCredential TokenCredential,
    string AccountEndpoint,
    string DatabaseId,
    string[] ContainerIds);
