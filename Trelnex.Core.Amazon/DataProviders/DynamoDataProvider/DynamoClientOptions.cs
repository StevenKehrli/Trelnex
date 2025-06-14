using Amazon.Runtime;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Configuration options for connecting to Amazon DynamoDB.
/// </summary>
/// <param name="AWSCredentials">The credentials used to authenticate DynamoDB requests.</param>
/// <param name="Region">The AWS region where the DynamoDB tables are located.</param>
/// <param name="TableNames">The collection of DynamoDB table names.</param>
/// <remarks>Used to establish a connection to DynamoDB using AWS credentials.</remarks>
internal record DynamoClientOptions(
    AWSCredentials AWSCredentials,
    string Region,
    string[] TableNames);
