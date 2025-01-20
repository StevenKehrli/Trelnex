using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

[Ignore("Requires a CosmosDB instance.")]
public class CosmosCommandProviderTests : CommandProviderTests
{
    private Container _container = null!;

    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // create a cosmos client for cleanup
        var tokenCredential = new DefaultAzureCredential();

        var endpointUri = configuration
            .GetSection("CosmosCommandProviders:EndpointUri")
            .Value!;

        var databaseId = configuration
            .GetSection("CosmosCommandProviders:DatabaseId")
            .Value!;

        var containerId = configuration
            .GetSection("CosmosCommandProviders:Containers:0:ContainerId")
            .Value!;

        var cosmosClient = new CosmosClient(
            accountEndpoint: endpointUri,
            tokenCredential: tokenCredential);

        _container = cosmosClient.GetContainer(
            databaseId: databaseId,
            containerId: containerId);

        // create the command provider
        var cosmosClientOptions = new Data.CosmosClientOptions(
            TokenCredential: tokenCredential,
            AccountEndpoint: endpointUri,
            DatabaseId: databaseId,
            ContainerIds: [ containerId ]
        );

        var keyResolverOptions = new KeyResolverOptions(
            TokenCredential: tokenCredential);

        var factory = await CosmosCommandProviderFactory.Create(
            cosmosClientOptions,
            keyResolverOptions);

        _commandProvider = factory.Create<ITestItem, TestItem>(
            containerId,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    [TearDown]
    public async Task TestCleanup()
    {
        // This method is called after each test case is run.

        var feedIterator = _container
            .GetItemLinqQueryable<CosmosItem>()
            .ToFeedIterator();

        while (feedIterator.HasMoreResults)
        {
            var feedResponse = await feedIterator.ReadNextAsync();

            foreach (var item in feedResponse)
            {
                await _container.DeleteItemAsync<CosmosItem>(
                    id: item.id,
                    partitionKey: new PartitionKey(item.partitionKey));
            }
        }
    }

    private record CosmosItem(
        string id,
        string partitionKey);
}
