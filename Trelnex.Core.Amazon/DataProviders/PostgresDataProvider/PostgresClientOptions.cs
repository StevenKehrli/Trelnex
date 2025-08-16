using Amazon;
using Amazon.Runtime;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Configuration settings for PostgreSQL database connection using AWS IAM authentication.
/// </summary>
/// <param name="AWSCredentials">AWS credentials for IAM-based database authentication.</param>
/// <param name="Region">AWS region where the PostgreSQL server is hosted.</param>
/// <param name="Host">PostgreSQL server hostname or IP address.</param>
/// <param name="Port">PostgreSQL server port number.</param>
/// <param name="Database">PostgreSQL database name to connect to.</param>
/// <param name="DbUser">Database username for authentication.</param>
/// <param name="TableNames">Array of table names that will be accessed by this client.</param>
internal record PostgresClientOptions(
    AWSCredentials AWSCredentials,
    RegionEndpoint Region,
    string Host,
    int Port,
    string Database,
    string DbUser,
    string[] TableNames);
