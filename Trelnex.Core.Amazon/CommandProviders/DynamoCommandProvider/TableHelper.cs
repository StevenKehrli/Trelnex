using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Trelnex.Core.Amazon.CommandProviders;

internal static class TableHelper
{
    public static Table GetTable(
        this AmazonDynamoDBClient dynamoClient,
        string tableName)
    {
        // create the table builder
        var tableBuilder = new TableBuilder(dynamoClient, tableName)
            .AddHashKey("partitionKey", DynamoDBEntryType.String)
            .AddRangeKey("id", DynamoDBEntryType.String);

        return tableBuilder.Build();
    }
}
