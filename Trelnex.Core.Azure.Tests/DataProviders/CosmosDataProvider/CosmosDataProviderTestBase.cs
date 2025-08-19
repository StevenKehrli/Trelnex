using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Base class for CosmosDataProvider tests containing shared functionality.
/// </summary>
/// <remarks>
/// This class provides common infrastructure for testing Cosmos data providers, including:
/// - Shared configuration loading
/// - Container management
/// - Test cleanup logic
/// </remarks>
public abstract class CosmosDataProviderTestBase : DataProviderTests
{
    /// <summary>
    /// The CosmosDB container used for testing.
    /// </summary>
    protected Container _container = null!;

    /// <summary>
    /// The endpoint URI for the Cosmos DB account.
    /// </summary>
    /// <example>https://cosmosdb-account.documents.azure.com:443/</example>
    protected string _endpointUri = null!;

    /// <summary>
    /// The database ID for the Cosmos DB database.
    /// </summary>
    /// <example>trelnex-core-data-tests</example>
    protected string _databaseId = null!;

    /// <summary>
    /// The container id used for testing.
    /// </summary>
    protected string _containerId = null!;

    /// <summary>
    /// The block cipher service used for encrypting and decrypting test data.
    /// </summary>
    protected IBlockCipherService _blockCipherService = null!;

    /// <summary>
    /// The service configuration containing application settings like name, version, and description.
    /// </summary>
    /// <remarks>
    /// This configuration is loaded from the ServiceConfiguration section in appsettings.json.
    /// </remarks>
    protected ServiceConfiguration _serviceConfiguration = null!;

    /// <summary>
    /// The token credential used to authenticate with Azure.
    /// </summary>
    protected DefaultAzureCredential _tokenCredential = null!;

    /// <summary>
    /// Initializes shared test resources from configuration.
    /// </summary>
    /// <returns>The loaded configuration</returns>
    protected IConfiguration TestSetup()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the service configuration from the configuration.
        _serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        // Get the endpoint URI from the configuration.
        // Example: "https://cosmosdataprovider-tests.documents.azure.com:443/"
        _endpointUri = configuration
            .GetSection("Azure.CosmosDataProviders:EndpointUri")
            .Get<string>()!;

        // Get the database ID from the configuration.
        // Example: "trelnex-core-data-tests"
        _databaseId = configuration
            .GetSection("Azure.CosmosDataProviders:DatabaseId")
            .Get<string>()!;

        // Get the container ID from the configuration.
        // Example: "test-items"
        var testItemContainerId = configuration
            .GetSection("Azure.CosmosDataProviders:Containers:test-item:ContainerId")
            .Get<string>()!;

        // Get the encypted container ID from the configuration.
        // Example: "test-items"
        var encryptedTestItemContainerId = configuration
            .GetSection("Azure.CosmosDataProviders:Containers:encrypted-test-item:ContainerId")
            .Get<string>()!;

        Assert.That(encryptedTestItemContainerId, Is.EqualTo(testItemContainerId));

        _containerId = testItemContainerId;

        // Create the block cipher service from configuration using the factory pattern.
        // This deserializes the algorithm type and settings, then creates the appropriate service.
        _blockCipherService = configuration
            .GetSection("Azure.CosmosDataProviders:Containers:encrypted-test-item")
            .CreateBlockCipherService()!;

        // Create a token credential for authentication.
        _tokenCredential = new DefaultAzureCredential();

        // Create a CosmosClient instance.
        var cosmosClient = new CosmosClient(
            accountEndpoint: _endpointUri,
            tokenCredential: _tokenCredential);

        // Get a reference to the container.
        _container = cosmosClient.GetContainer(
            databaseId: _databaseId,
            containerId: _containerId);

        return configuration;
    }

    /// <summary>
    /// Cleans up the CosmosDB container after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the CosmosDB container
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public async Task TearDown()
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

    /// <summary>
    /// Record representing a minimal CosmosDB item used for cleanup operations.
    /// </summary>
    /// <param name="id">The id of the CosmosDB item.</param>
    /// <param name="partitionKey">The partition key of the CosmosDB item.</param>
    protected record CosmosItem(
        string id,
        string partitionKey);
}
