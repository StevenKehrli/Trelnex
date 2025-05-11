using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// Helper methods for working with DynamoDB tables.
/// </summary>
/// <remarks>
/// Provides extension methods to create <see cref="Table"/> objects.
/// </remarks>
internal static class TableHelper
{
    #region Public Static Methods

    /// <summary>
    /// Gets a DynamoDB <see cref="Table"/> object with a standard key schema.
    /// </summary>
    /// <param name="dynamoClient">The DynamoDB client.</param>
    /// <param name="tableName">The name of the DynamoDB table.</param>
    /// <returns>A configured <see cref="Table object.</returns>
    /// <remarks>
    /// Builds a <see cref="Table"/> object with a composite key.
    /// </remarks>
    public static Table GetTable(
        this AmazonDynamoDBClient dynamoClient,
        string tableName)
    {
        // Create a table builder with a standard key schema.
        var tableBuilder = new TableBuilder(dynamoClient, tableName)
            .AddHashKey("partitionKey", DynamoDBEntryType.String)
            .AddRangeKey("id", DynamoDBEntryType.String);

        // Build and return the <see cref="Table object.
        return tableBuilder.Build();
    }

    #endregion
}
