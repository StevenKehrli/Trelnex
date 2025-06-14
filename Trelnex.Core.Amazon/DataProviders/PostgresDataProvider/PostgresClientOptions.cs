using Amazon;
using Amazon.Runtime;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Configuration options for connecting to PostgreSQL.
/// </summary>
/// <param name="AWSCredentials">The credentials used to authenticate PostgreSQL requests.</param>
/// <param name="Region">The AWS region where the PostgreSQL server is hosted.</param>
/// <param name="Host">The hostname or IP address of the PostgreSQL server.</param>
/// <param name="Port">The port number on which the PostgreSQL server is listening.</param>
/// <param name="Database">The name of the PostgreSQL database to connect to.</param>
/// <param name="DbUser">The database user name to authenticate with.</param>
/// <param name="TableNames">The collection of table names in the database that will be accessed.</param>
/// <remarks>Used to establish a connection to PostgreSQL using AWS IAM authentication.</remarks>
internal record PostgresClientOptions(
    AWSCredentials AWSCredentials,
    RegionEndpoint Region,
    string Host,
    int Port,
    string Database,
    string DbUser,
    string[] TableNames);
