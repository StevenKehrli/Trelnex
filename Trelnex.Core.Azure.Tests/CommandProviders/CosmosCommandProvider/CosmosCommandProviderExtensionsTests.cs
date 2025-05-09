using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the extension methods used to register and configure CosmosCommandProviders
/// in the dependency injection container.
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
/// implementation specifically, focusing on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosCommandProvider")]
public class CosmosCommandProviderExtensionsTests : CommandProviderTests
{
    /// <summary>
    /// The CosmosDB container used for testing.
    /// </summary>
    private Container _container = null!;

    /// <summary>
    /// Sets up the CosmosCommandProvider for testing using the dependency injection approach.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It uses the DI extensions to register
    /// the provider, simulating how it would be used in a real application.
    ///
    /// The setup process involves:
    /// <list type="number">
    /// <item>Creating a service collection</item>
    /// <item>Loading configuration from appsettings.json</item>
    /// <item>Creating a CosmosDB client for test cleanup</item>
    /// <item>Configuring Serilog and Azure identity</item>
    /// <item>Registering the CosmosCommandProvider with DI extensions</item>
    /// <item>Building the service provider and retrieving the command provider</item>
    /// </list>
    /// </remarks>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the service configuration from the configuration.
        var serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        services.AddSingleton(serviceConfiguration);

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
            tokenCredential: new DefaultAzureCredential());

        // Get a reference to the container.
        _container = cosmosClient.GetContainer(
            databaseId: databaseId,
            containerId: containerId);

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration()
            {
                FullName = "CosmosCommandProviderExtensionsTests",
                DisplayName = "CosmosCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "CosmosCommandProviderExtensionsTests",
            });

        // Add Azure Identity and Cosmos Command Providers to the service collection.
        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddCosmosCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the command provider from the DI container.
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
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
    /// Tests that registering the same type with the CosmosCommandProvider twice results in an exception.
    /// </summary>
    /// <remarks>
    /// This test verifies that the registration extension properly detects and prevents duplicate
    /// registrations of the same entity type, which would lead to ambiguous resolution in the DI container.
    /// </remarks>
    [Test]
    [Description("Tests that registering the same type with the CosmosCommandProvider twice results in an exception.")]
    public void CosmosCommandProvider_AlreadyRegistered()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration()
            {
                FullName = "CosmosCommandProviderExtensionsTests",
                DisplayName = "CosmosCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "CosmosCommandProviderExtensionsTests",
            });

        // Attempt to register the same type twice, which should throw an InvalidOperationException.
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddCosmosCommandProviders(
                configuration,
                bootstrapLogger,
                options => options
                    .Add<ITestItem, TestItem>(
                        typeName: "test-item",
                        validator: TestItem.Validator,
                        commandOperations: CommandOperations.All)
                    .Add<ITestItem, TestItem>(
                        typeName: "test-item",
                        validator: TestItem.Validator,
                        commandOperations: CommandOperations.All));
        });
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
