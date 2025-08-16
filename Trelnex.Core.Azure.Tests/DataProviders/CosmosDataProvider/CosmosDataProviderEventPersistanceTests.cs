using System.Dynamic;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for <see cref="CosmosDataProvider"/> focusing on event persistence functionality.
/// </summary>
/// <remarks>
/// Inherits from <see cref="CosmosDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live CosmosDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosDataProvider")]
public class CosmosDataProviderEventPersistanceTests : CosmosDataProviderEventTestBase
{
    /// <summary>
    /// One-time setup for the test fixture.
    /// Initializes the CosmosDataProvider using direct factory instantiation,
    /// configuring event expiration for test items.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared test resources from configuration
        TestSetup();

        // Configure CosmosClientOptions with credentials and container info
        var cosmosClientOptions = new CosmosClientOptions(
            TokenCredential: _tokenCredential,
            AccountEndpoint: _endpointUri,
            DatabaseId: _databaseId,
            ContainerIds: [ _persistenceContainerId ]
        );

        // Instantiate the CosmosDataProviderFactory
        var factory = await CosmosDataProviderFactory.Create(
            cosmosClientOptions);

        // Create the data provider with event expiration set to 2 seconds
        _dataProvider = factory.Create(
            typeName: "test-item",
            containerId: _persistenceContainerId,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All);
    }

    [Test]
    [Description("Tests events stored by CosmosDataProvider do not expire.")]
    public async Task CosmosDataProvider_WithoutExpiration()
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
        var item1 = await _persistenceContainer.ReadItemAsync<ExpandoObject>(
            id: eventId,
            partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item1, Is.Not.Null);
        Assert.That(item1.Resource, Is.Not.Null);

        var dictionary1 = item1.Resource as IDictionary<string, object>;
        Assert.That(dictionary1.ContainsKey("ttl"), Is.False);
    }
}
