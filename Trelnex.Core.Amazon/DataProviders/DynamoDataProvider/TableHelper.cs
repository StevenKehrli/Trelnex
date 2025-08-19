using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Extension methods for creating DynamoDB Table objects with standard key schemas.
/// </summary>
internal static class TableHelper
{
    #region Public Static Methods

    /// <summary>
    /// Creates a DynamoDB Table object configured with the standard composite key schema.
    /// </summary>
    /// <param name="dynamoClient">DynamoDB client for table operations.</param>
    /// <param name="tableName">Name of the DynamoDB table.</param>
    /// <returns>Configured Table object with partitionKey as hash key and id as range key.</returns>
    public static Table GetTable(
        this AmazonDynamoDBClient dynamoClient,
        string tableName)
    {
        // Configure table with composite key: partitionKey (hash) + id (range)
        var tableBuilder = new TableBuilder(dynamoClient, tableName)
            .AddHashKey("partitionKey", DynamoDBEntryType.String)
            .AddRangeKey("id", DynamoDBEntryType.String);

        return tableBuilder.Build();
    }

    #endregion
}
