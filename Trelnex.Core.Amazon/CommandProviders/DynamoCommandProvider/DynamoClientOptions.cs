using Amazon.Runtime;

namespace Trelnex.Core.Amazon.CommandProviders;

internal record DynamoClientOptions(
    AWSCredentials AWSCredentials,
    string Region,
    string[] TableNames);
