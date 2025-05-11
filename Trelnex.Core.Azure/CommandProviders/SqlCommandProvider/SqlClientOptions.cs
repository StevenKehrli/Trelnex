using Azure.Core;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Configuration options for connecting to SQL Server.
/// </summary>
/// <param name="TokenCredential">The credential used to authenticate SQL Server requests.</param>
/// <param name="Scope">The authentication scope for the token request.</param>
/// <param name="DataSource">The server name or network address.</param>
/// <param name="InitialCatalog">The database name.</param>
/// <param name="TableNames">The collection of table names in the database.</param>
/// <remarks>Parameters to connect to SQL Server using token-based authentication.</remarks>
internal record SqlClientOptions(
    TokenCredential TokenCredential,
    string Scope,
    string DataSource,
    string InitialCatalog,
    string[] TableNames);
