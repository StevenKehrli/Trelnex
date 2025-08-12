using System.Dynamic;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for <see cref="CosmosDataProvider"/> using dependency injection, focusing on event expiration functionality.
/// </summary>
/// <remarks>
/// Inherits from <see cref="CosmosDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live CosmosDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosDataProvider")]
public class CosmosDataProviderExtensionsEventExpirationTests : CosmosDataProviderEventTestBase
{
    /// <summary>
    /// Sets up the CosmosDataProvider for testing using the dependency injection approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Initialize shared resources from configuration
        var configuration = TestSetup();

        services.AddSingleton(_serviceConfiguration);

        var bootstrapLogger = services.AddSerilog(
            configuration,
            _serviceConfiguration);

        // Add Azure Identity and Cosmos Data providers to the service collection.
        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddCosmosDataProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "expiration-test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider from the DI container.
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<ITestItem>>();
    }

    [Test]
    [Description("Tests events stored by dependency injected CosmosDataProvider expire and are deleted after the configured TTL.")]
    public async Task CosmosDataProviderExtensions_WithExpiration()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create a command for a new test item
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Populate the test item with sample messages
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";
        createCommand.Item.OptionalMessage = "Optional Message #1";

        // Save the item and verify creation
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        // Immediately retrieve the event to confirm it exists
        var eventId = $"EVENT^^{id}^00000001";
        var item1 = await _expirationContainer.ReadItemAsync<ExpandoObject>(
            id: eventId,
            partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item1, Is.Not.Null);
        Assert.That(item1.Resource, Is.Not.Null);

        var dictionary1 = item1.Resource as IDictionary<string, object>;
        Assert.That(dictionary1["ttl"], Is.EqualTo(2));

        // Wait for the event expiration period (TTL)
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Attempt to retrieve the event again, expecting it to be expired and deleted
        var exception = Assert.ThrowsAsync<Microsoft.Azure.Cosmos.CosmosException>(async () =>
            await _expirationContainer.ReadItemAsync<ExpandoObject>(
                id: eventId,
                partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
                cancellationToken: default));

        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
