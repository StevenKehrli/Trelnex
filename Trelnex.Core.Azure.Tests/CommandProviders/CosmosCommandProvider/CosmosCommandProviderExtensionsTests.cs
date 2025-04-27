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

// [Ignore("Requires a CosmosDB instance.")]
public class CosmosCommandProviderExtensionsTests : CommandProviderTests
{
    private Container _container = null!;

    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // This method is called once prior to executing any of the tests in the fixture.

        // create the service collection
        var services = new ServiceCollection();

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

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() {
                FullName = "CosmosCommandProviderExtensionsTests",
                DisplayName = "CosmosCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "CosmosCommandProviderExtensionsTests",
            });

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

        // get the command provider
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
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

    [Test]
    public void CosmosCommandProvider_AlreadyRegistered()
    {
        // create the service collection
        var services = new ServiceCollection();

        // create the test configuration
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        var bootstrapLogger = services.AddSerilog(
            configuration,
            new ServiceConfiguration() {
                FullName = "CosmosCommandProviderExtensionsTests",
                DisplayName = "CosmosCommandProviderExtensionsTests",
                Version = "0.0.0",
                Description = "CosmosCommandProviderExtensionsTests",
            });

        // add twice
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

    private record CosmosItem(
        string id,
        string partitionKey);
}
