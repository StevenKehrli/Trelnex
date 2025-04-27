using Amazon.Runtime;

namespace Trelnex.Core.Amazon.CommandProviders;

internal record PostgresClientOptions(
    AWSCredentials AWSCredentials,
    string Region,
    string Host,
    int Port,
    string Database,
    string DbUser,
    string[] TableNames);
