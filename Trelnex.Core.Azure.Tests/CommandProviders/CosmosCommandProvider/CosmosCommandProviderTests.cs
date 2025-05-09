using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the CosmosCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CommandProviderTests"/> to leverage the extensive test suite
/// defined in the base class. The base class implements a comprehensive set of tests for command provider
/// functionality including:
/// <list type="bullet">
/// <item>Batch command operations (create, update, delete with success and failure scenarios)</item>
/// <item>Create command operations (with success and conflict handling)</item>
/// <item>Delete command operations (with success and precondition failure handling)</item>
/// <item>Query command operations (with various filters, ordering, paging)</item>
/// <item>Read command operations</item>
/// <item>Update command operations (with success and precondition failure handling)</item>
/// </list>
///
/// By inheriting from CommandProviderTests, this class runs all those tests against the CosmosCommandProvider
/// implementation specifically, using direct factory instantiation rather than dependency injection.
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosCommandProvider")]
public class CosmosCommandProviderTests : CommandProviderTests
{
    /// <summary>
    /// The CosmosDB container used for testing.
    /// </summary>
    private Container _container = null!;

    /// <summary>
    /// Sets up the CosmosCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It uses direct factory instantiation,
    /// which tests the core functionality without the dependency injection layer.
    ///
    /// The setup process involves:
    /// <list type="number">
    /// <item>Loading configuration from appsettings.json</item>
    /// <item>Creating a CosmosDB client for test cleanup</item>
    /// <item>Creating CosmosClientOptions and KeyResolverOptions</item>
    /// <item>Creating the CosmosCommandProviderFactory</item>
    /// <item>Using the factory to create a specific command provider instance</item>
    /// </list>
    /// </remarks>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Create the test configuration. We expect to load settings from appsettings.json and appsettings.User.json.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Create a cosmos client for cleanup.
        var tokenCredential = new DefaultAzureCredential();

        // Get the endpoint URI from the configuration.
        // Example: "https://cosmoscommandprovider-tests.documents.azure.com:443/"
        var endpointUri = configuration
            .GetSection("CosmosCommandProviders:EndpointUri")
            .Value!;

        // Get the database ID from the configuration.
        // Example: "trelnex-core-data-tests"
        var databaseId = configuration
            .GetSection("CosmosCommandProviders:DatabaseId")
            .Value!;

        // Get the container ID from the configuration.
        // Example: "test-items"
        var containerId = configuration
            .GetSection("CosmosCommandProviders:Containers:0:ContainerId")
            .Value!;

        // Create a CosmosClient instance.
        var cosmosClient = new CosmosClient(
            accountEndpoint: endpointUri,
            tokenCredential: tokenCredential);

        // Get a reference to the container.
        _container = cosmosClient.GetContainer(
            databaseId: databaseId,
            containerId: containerId);

        // Create the command provider using direct factory instantiation.
        var cosmosClientOptions = new Azure.CommandProviders.CosmosClientOptions(
            TokenCredential: tokenCredential,
            AccountEndpoint: endpointUri,
            DatabaseId: databaseId,
            ContainerIds: [ containerId ]
        );

        var keyResolverOptions = new KeyResolverOptions(
            TokenCredential: tokenCredential);

        // Create the CosmosCommandProviderFactory.
        var factory = await CosmosCommandProviderFactory.Create(
            cosmosClientOptions,
            keyResolverOptions);

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            containerId,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    /// <summary>
    /// Cleans up the CosmosDB container after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the CosmosDB container
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    ///
    /// The cleanup process involves:
    /// <list type="number">
    /// <item>Querying all items in the container</item>
    /// <item>Iterating through the results in batches</item>
    /// <item>Deleting each item individually</item>
    /// </list>
    /// </remarks>
    [TearDown]
    public async Task TestCleanup()
    {
        // Query all items in the container.
        var feedIterator = _container
            .GetItemLinqQueryable<CosmosItem>()
            .ToFeedIterator();

        // Iterate through the results in batches.
        while (feedIterator.HasMoreResults)
        {
            var feedResponse = await feedIterator.ReadNextAsync();

            // Delete each item individually.
            foreach (var item in feedResponse)
            {
                await _container.DeleteItemAsync<CosmosItem>(
                    id: item.id,
                    partitionKey: new PartitionKey(item.partitionKey));
            }
        }
    }

    /// <summary>
    /// Record representing a minimal CosmosDB item used for cleanup operations.
    /// </summary>
    /// <param name="id">The ID of the CosmosDB item.</param>
    /// <param name="partitionKey">The partition key of the CosmosDB item.</param>
    private record CosmosItem(
        string id,
        string partitionKey);
}
