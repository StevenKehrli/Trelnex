using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.PropertyChanges;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.PropertyChanges;

[Ignore("Requires a DynamoDB table.")]
[Category("EventPolicy")]
public class DynamoDataProviderTests : EventPolicyTests
{
    private Table _itemTable = null!;
    private Table _eventTable = null!;
    private DynamoDataProviderFactory _factory = null!;

    /// <summary>
    /// Sets up the DynamoDataProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the region from the configuration.
        // Example: "us-west-2"
        var region = configuration
            .GetSection("Amazon.DynamoDataProviders:Region")
            .Get<string>()!;

        // Get the item table name from the configuration.
        // Example: "test-items"
        var itemTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:test-item:ItemTableName")
            .Get<string>()!;

        // Get the event table name from the configuration.
        // Example: "test-items-events"
        var eventTableName = configuration
            .GetSection("Amazon.DynamoDataProviders:Tables:test-item:EventTableName")
            .Get<string>()!;

        // Create AWS credentials
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Create a DynamoDB client for cleanup
        var dynamoClient = new AmazonDynamoDBClient(
            awsCredentials,
            RegionEndpoint.GetBySystemName(region));

        // Create the data provider using direct factory instantiation.
        var dynamoClientOptions = new DynamoClientOptions(
            AWSCredentials: awsCredentials,
            Region: region,
            TableNames: [ itemTableName, eventTableName ]
        );

        _itemTable = dynamoClient.GetTable(itemTableName);
        _eventTable = dynamoClient.GetTable(eventTableName);

        _factory = await DynamoDataProviderFactory.Create(
            dynamoClientOptions: dynamoClientOptions);
    }

    /// <summary>
    /// Cleans up the DynamoDB table after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the DynamoDB table
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public async Task TestCleanup()
    {
        await TableCleanup(_eventTable);
        await TableCleanup(_itemTable);
    }

    private static async Task TableCleanup(
        Table table)
    {
        // Create a scan filter to find all documents in the table.
        var scanFilter = new ScanFilter();
        var search = table.Scan(scanFilter);

        // Iterate through the results in batches.
        do
        {
            var documents = await search.GetNextSetAsync();

            // Delete each document individually.
            foreach (var document in documents)
            {
                await table.DeleteItemAsync(document);
            }
        } while (search.IsDone is false);
    }

    protected override Task<IDataProvider<EventPolicyTestItem>> GetDataProviderAsync(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null)
    {
        var dataProvider = _factory.Create<EventPolicyTestItem>(
            typeName: typeName,
            itemTableName: _itemTable.TableName,
            eventTableName: _eventTable.TableName,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService);
        
        return Task.FromResult(dataProvider);
    }

    protected override async Task<ItemEvent[]> GetItemEventsAsync(
        string id,
        string partitionKey)
    {
        var scanFilter = new ScanFilter();
        scanFilter.AddCondition("relatedId", ScanOperator.Equal, id);
        scanFilter.AddCondition("partitionKey", ScanOperator.Equal, partitionKey);

        var search = _eventTable.Scan(scanFilter);

        var results = new List<ItemEvent>();

        while (search.IsDone is false)
        {
            var documents = await search.GetNextSetAsync();

            foreach (var document in documents)
            {
                var json = document.ToJson();
                var itemEvent = JsonSerializer.Deserialize<ItemEvent>(json)!;

                results.Add(itemEvent);
            }
        }

        return results.ToArray();
    }
}
