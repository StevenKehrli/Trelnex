using Azure.Core;

namespace Trelnex.Core.Data;

public record SqlClientOptions(
    TokenCredential TokenCredential,
    string Scope,
    string DataSource,
    string InitialCatalog,
    string[] TableNames);
