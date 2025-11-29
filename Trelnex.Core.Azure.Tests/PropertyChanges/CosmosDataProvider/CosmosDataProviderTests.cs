using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.PropertyChanges;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.PropertyChanges;

[Ignore("Requires a CosmosDB instance.")]
[Category("EventPolicy")]
public class CosmosDataProviderTests : EventPolicyTests
{
    private Container _container = null!;

    /// <summary>
    /// Sets up the CosmosDataProvider for testing using the direct constructor instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the endpoint URI from the configuration.
        // Example: "https://cosmosdataprovider-tests.documents.azure.com:443/"
        var endpointUri = configuration
            .GetSection("Azure.CosmosDataProviders:EndpointUri")
            .Get<string>();

        // Get the database ID from the configuration.
        // Example: "trelnex-core-data-tests"
        var databaseId = configuration
            .GetSection("Azure.CosmosDataProviders:DatabaseId")
            .Get<string>();

        // Get the container ID from the configuration.
        // Example: "test-items"
        var containerId = configuration
            .GetSection("Azure.CosmosDataProviders:Containers:test-item:ContainerId")
            .Get<string>();

        // Create a token credential for authentication.
        var tokenCredential = new DefaultAzureCredential();

        var cosmosClient = new CosmosClient(
            accountEndpoint: endpointUri,
            tokenCredential: tokenCredential,
            clientOptions: new CosmosClientOptions
            {
                Serializer = new SystemTextJsonSerializer()
            });

        // Get a reference to the container.
        _container = cosmosClient.GetContainer(
            databaseId: databaseId,
            containerId: containerId);
    }

    /// <summary>
    /// Cleans up the CosmosDB container after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the CosmosDB container
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public async Task TestCleanup()
    {
        await ContainerCleanup(_container);
    }

    private static async Task ContainerCleanup(
        Container container)
    {
        // Query all items in the container.
        var feedIterator = container
            .GetItemLinqQueryable<CosmosItem>()
            .ToFeedIterator();

        // Iterate through the results in batches.
        while (feedIterator.HasMoreResults)
        {
            var feedResponse = await feedIterator.ReadNextAsync();

            // Delete each item individually.
            foreach (var item in feedResponse)
            {
                await container.DeleteItemAsync<CosmosItem>(
                    id: item.id,
                    partitionKey: new PartitionKey(item.partitionKey));
            }
        }
    }

    protected override Task<IDataProvider<EventPolicyTestItem>> GetDataProviderAsync(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null)
    {
        var dataProvider = new CosmosDataProvider<EventPolicyTestItem>(
            typeName: typeName,
            container: _container,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService);

        return Task.FromResult<IDataProvider<EventPolicyTestItem>>(dataProvider);
    }

    protected override async Task<ItemEvent[]> GetItemEventsAsync(
        string id,
        string partitionKey)
    {
        // Query all events in the container.
        var feedIterator = _container
            .GetItemLinqQueryable<ItemEvent>()
            .Where(item => item.RelatedId == id && item.PartitionKey == partitionKey)
            .ToFeedIterator();

        var results = new List<ItemEvent>();
        while (feedIterator.HasMoreResults)
        {
            var feedResponse = await feedIterator.ReadNextAsync();

            foreach (var itemEvent in feedResponse)
            {
                results.Add(itemEvent);

            }
        }

        return results.ToArray();
    }

    /// <summary>
    /// Record representing a minimal CosmosDB item used for cleanup operations.
    /// </summary>
    /// <param name="id">The id of the CosmosDB item.</param>
    /// <param name="partitionKey">The partition key of the CosmosDB item.</param>
    protected record CosmosItem(
        string id,
        string partitionKey);
}
