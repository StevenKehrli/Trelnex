using System.Dynamic;
using System.Net;
using Microsoft.Azure.Cosmos;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

using CosmosClientOptions = Trelnex.Core.Azure.DataProviders.CosmosClientOptions;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for <see cref="CosmosDataProvider"/> focusing on event expiration functionality.
/// </summary>
/// <remarks>
/// Inherits from <see cref="CosmosDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live CosmosDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosDataProvider")]
public class CosmosDataProviderEventExpirationTests : CosmosDataProviderEventTestBase
{
    /// <summary>
    /// One-time setup for the test fixture.
    /// Initializes the CosmosDataProvider using direct factory instantiation,
    /// configuring event expiration for test items.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        TestSetup();

        // Configure CosmosClientOptions with credentials and container info
        var cosmosClientOptions = new CosmosClientOptions(
            TokenCredential: _tokenCredential,
            AccountEndpoint: _endpointUri,
            DatabaseId: _databaseId,
            ContainerIds: [ _containerId ]
        );

        // Instantiate the CosmosDataProviderFactory
        var factory = await CosmosDataProviderFactory.Create(
            cosmosClientOptions);

        // Create the data provider with event expiration set to 2 seconds
        _dataProvider = factory.Create(
            typeName: "expiration-test-item",
            containerId: _containerId,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            eventTimeToLive: 2);
    }

    /// <summary>
    /// Verifies that events created via CosmosDataProvider expire and are no longer retrievable after the configured TTL.
    /// </summary>
    /// <remarks>
    /// The test creates a test item, verifies the event exists immediately after creation,
    /// checks the TTL property, waits for the expiration period, and asserts that a NotFound exception is thrown when attempting to retrieve the expired event.
    /// </remarks>
    [Test]
    [Description("Tests events stored by CosmosDataProvider expire and are deleted after the configured TTL.")]
    public async Task CosmosDataProvider_WithExpiration()
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
        var eventId = $"EVENT^{id}^00000001";
        var item1 = await _container.ReadItemAsync<ExpandoObject>(
            id: eventId,
            partitionKey: new PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item1, Is.Not.Null);
        Assert.That(item1.Resource, Is.Not.Null);

        var dictionary1 = item1.Resource as IDictionary<string, object>;
        Assert.That(dictionary1["ttl"], Is.EqualTo(2));

        // Wait for the event expiration period (TTL)
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Attempt to retrieve the event again, expecting it to be expired and deleted
        var exception = Assert.ThrowsAsync<CosmosException>(async () =>
            await _container.ReadItemAsync<ExpandoObject>(
                id: eventId,
                partitionKey: new PartitionKey(partitionKey),
                cancellationToken: default));

        Assert.That(exception.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
