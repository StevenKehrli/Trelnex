using Azure.Core;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Configuration settings for SQL Server database connection using Azure token authentication.
/// </summary>
/// <param name="TokenCredential">Azure credential for authenticating SQL Server requests.</param>
/// <param name="Scope">Authentication scope for token requests.</param>
/// <param name="DataSource">SQL Server instance name or network address.</param>
/// <param name="InitialCatalog">SQL Server database name to connect to.</param>
/// <param name="TableNames">Array of table names that will be accessed by this client.</param>
internal record SqlClientOptions(
    TokenCredential TokenCredential,
    string Scope,
    string DataSource,
    string InitialCatalog,
    string[] TableNames);
