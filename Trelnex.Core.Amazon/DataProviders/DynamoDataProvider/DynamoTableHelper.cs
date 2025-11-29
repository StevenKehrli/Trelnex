using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// Extension methods for creating DynamoDB Table objects with standard key schemas.
/// </summary>
internal static class DynamoTableHelper
{
    #region Public Static Methods

    /// <summary>
    /// Creates a DynamoDB Table object by discovering the table's key schema and GSI configuration via DescribeTable.
    /// </summary>
    /// <param name="dynamoClient">DynamoDB client for table operations.</param>
    /// <param name="logger">Logger for diagnostic and debugging information.</param>
    /// <param name="tableName">Name of the DynamoDB table.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Configured Table object with the correct hash key, range key, and Global Secondary Indexes based on table description.</returns>
    /// <remarks>
    /// <para>
    /// This method performs a DescribeTable operation to dynamically discover the table's schema,
    /// including primary key structure and all Global Secondary Index configurations.
    /// </para>
    /// <para>
    /// Supports tables with hash-only or hash+range primary keys, and GSIs with similar key structures.
    /// Automatically converts DynamoDB scalar attribute types (S, N, B) to appropriate DynamoDBEntryType values.
    /// </para>
    /// </remarks>
    public static async Task<Table> LoadTableAsync(
        this IAmazonDynamoDB dynamoClient,
        ILogger logger,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "DynamoTableHelper.LoadTableAsync Enter: Table = '{TableName}'.",
            tableName);

        // Describe the table to get its key schema, attribute definitions, and GSI configurations
        var describeTableResponse = await dynamoClient.DescribeTableAsync(
            tableName,
            cancellationToken);

        var tableKeySchema = describeTableResponse.Table.KeySchema;
        var tableAttributeDefinitions = describeTableResponse.Table.AttributeDefinitions;

        var tableBuilder = new TableBuilder(
            ddbClient: dynamoClient,
            tableName: tableName,
            conversion: DynamoDBEntryConversion.V2,
            isEmptyStringValueEnabled: true,
            metadataCachingMode: null);

        // Configure primary table hash key (partition key)
        var tableHashKey = tableKeySchema.First(k => k.KeyType == KeyType.HASH);
        var tableHashKeyAttributeDefinition = tableAttributeDefinitions.First(tad => tad.AttributeName == tableHashKey.AttributeName);
        var tableHashKeyType = tableHashKeyAttributeDefinition.AttributeType.ToDynamoDBEntryType();

        tableBuilder.AddHashKey(
            hashKeyAttribute: tableHashKey.AttributeName,
            type: tableHashKeyType);

        // Configure primary table range key (sort key) if it exists
        var tableRangeKey = tableKeySchema.Find(k => k.KeyType == KeyType.RANGE);
        if (tableRangeKey is null)
        {
            logger.LogDebug(
                "Added Table '{TableName}' Primary Key: HashKey = '{HashKey} ({HashKeyType})'.",
                tableName,
                tableHashKey.AttributeName,
                tableHashKeyType);
        }
        else
        {
            var tableRangeKeyAttributeDefinition = tableAttributeDefinitions.First(tad => tad.AttributeName == tableRangeKey.AttributeName);
            var tableRangeKeyType = tableRangeKeyAttributeDefinition.AttributeType.ToDynamoDBEntryType();

            tableBuilder.AddRangeKey(
                rangeKeyAttribute: tableRangeKey.AttributeName,
                type: tableRangeKeyType);

            logger.LogDebug(
                "Added Table '{TableName}' Primary Key: HashKey = '{HashKey} ({HashKeyType})', RangeKey = '{RangeKey} ({RangeKeyType})'.",
                tableName,
                tableHashKey.AttributeName,
                tableHashKeyType,
                tableRangeKey.AttributeName,
                tableRangeKeyType);
        }

        // Configure Global Secondary Indexes if they exist
        describeTableResponse.Table.GlobalSecondaryIndexes?.ForEach(gsi =>
        {
            // Extract GSI hash key and its attribute definition
            var gsiHashKey = gsi.KeySchema.First(k => k.KeyType == KeyType.HASH);
            var gsiHashKeyAttributeDefinition = tableAttributeDefinitions.First(tad => tad.AttributeName == gsiHashKey.AttributeName);
            var gsiHashKeyType = gsiHashKeyAttributeDefinition.AttributeType.ToDynamoDBEntryType();

            // Check if GSI has a range key
            var gsiRangeKey = gsi.KeySchema.Find(k => k.KeyType == KeyType.RANGE);
            if (gsiRangeKey is null)
            {
                // GSI with hash key only
                tableBuilder.AddGlobalSecondaryIndex(
                    indexName: gsi.IndexName,
                    hashkeyAttribute: gsiHashKey.AttributeName,
                    hashKeyType: gsiHashKeyType);

                logger.LogDebug(
                    "Added Table '{TableName}' GSI '{IndexName}': HashKey = '{HashKey} ({HashKeyType})'.",
                    tableName,
                    gsi.IndexName,
                    gsiHashKey.AttributeName,
                    gsiHashKeyType);
            }
            else
            {
                // GSI with both hash key and range key
                var gsiRangeKeyAttributeDefinition = tableAttributeDefinitions.First(tad => tad.AttributeName == gsiRangeKey.AttributeName);
                var gsiRangeKeyType = gsiRangeKeyAttributeDefinition.AttributeType.ToDynamoDBEntryType();

                tableBuilder.AddGlobalSecondaryIndex(
                    indexName: gsi.IndexName,
                    hashkeyAttribute: gsiHashKey.AttributeName,
                    hashKeyType: gsiHashKeyType,
                    rangeKeyAttribute: gsiRangeKey.AttributeName,
                    rangeKeyType: gsiRangeKeyType);

                logger.LogDebug(
                    "Added Table '{TableName}' GSI '{IndexName}': HashKey = '{HashKey} ({HashKeyType})', RangeKey = '{RangeKey} ({RangeKeyType})'.",
                    tableName,
                    gsi.IndexName,
                    gsiHashKey.AttributeName,
                    gsiHashKeyType,
                    gsiRangeKey.AttributeName,
                    gsiRangeKeyType);
            }
        });

        logger.LogDebug(
            "DynamoTableHelper.LoadTableAsync Exit: Table = '{TableName}'.",
            tableName);

        return tableBuilder.Build();
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Converts DynamoDB ScalarAttributeType to DynamoDBEntryType for TableBuilder configuration.
    /// </summary>
    /// <param name="attributeType">The DynamoDB scalar attribute type (S, N, or B).</param>
    /// <returns>Corresponding DynamoDBEntryType for use with TableBuilder.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported attribute type is provided.</exception>
    /// <remarks>
    /// Supports the three DynamoDB scalar types:
    /// - S (String) → DynamoDBEntryType.String
    /// - N (Number) → DynamoDBEntryType.Numeric
    /// - B (Binary) → DynamoDBEntryType.Binary
    /// </remarks>
    private static DynamoDBEntryType ToDynamoDBEntryType(
        this ScalarAttributeType attributeType)
    {
        // Convert DynamoDB attribute type to Document Model entry type
        if (attributeType == ScalarAttributeType.S) return DynamoDBEntryType.String;
        if (attributeType == ScalarAttributeType.N) return DynamoDBEntryType.Numeric;
        if (attributeType == ScalarAttributeType.B) return DynamoDBEntryType.Binary;

        throw new ArgumentException($"Unsupported attribute type: {attributeType}");
    }

    #endregion
}
