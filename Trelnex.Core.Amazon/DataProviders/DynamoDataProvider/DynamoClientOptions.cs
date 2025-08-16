using Amazon.Runtime;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Configuration settings for DynamoDB client connection and table access.
/// </summary>
/// <param name="AWSCredentials">AWS credentials for authenticating DynamoDB requests.</param>
/// <param name="Region">AWS region containing the DynamoDB tables.</param>
/// <param name="TableNames">Array of DynamoDB table names managed by this client.</param>
internal record DynamoClientOptions(
    AWSCredentials AWSCredentials,
    string Region,
    string[] TableNames);
