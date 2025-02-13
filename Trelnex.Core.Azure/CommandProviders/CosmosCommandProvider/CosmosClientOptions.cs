using Azure.Core;

namespace Trelnex.Core.Azure.CommandProviders;

internal record CosmosClientOptions(
    TokenCredential TokenCredential,
    string AccountEndpoint,
    string DatabaseId,
    string[] ContainerIds);
