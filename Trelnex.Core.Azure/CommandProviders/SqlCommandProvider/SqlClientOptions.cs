using Azure.Core;

namespace Trelnex.Core.Azure.CommandProviders;

internal record SqlClientOptions(
    TokenCredential TokenCredential,
    string Scope,
    string DataSource,
    string InitialCatalog,
    string[] TableNames);
