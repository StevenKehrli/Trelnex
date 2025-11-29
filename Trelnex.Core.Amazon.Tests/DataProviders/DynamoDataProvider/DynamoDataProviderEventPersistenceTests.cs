using Amazon.DynamoDBv2.DocumentModel;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the DynamoDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// Inherits from <see cref="DynamoDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live DynamoDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoDataProvider")]
public class DynamoDataProviderEventPersistenceTests : DynamoDataProviderEventTestBase
{
    /// <summary>
    /// Sets up the DynamoDataProvider for testing using direct constructor instantiation.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration and load tables
        await TestSetupAsync();

        // Create the data provider using constructor
        _dataProvider = new DynamoDataProvider<TestItem>(
            typeName: "test-item",
            itemTable: _itemTable,
            eventTable: _eventTable,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            eventPolicy: EventPolicy.AllChanges);
    }

    [Test]
    [Description("Tests events stored by DynamoDataProvider do not expire.")]
    public async Task DynamoDataProvider_WithoutExpiration()
    {
        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create a command for creating a test item
        using var createCommand = _dataProvider.Create(
            id: id,
            partitionKey: partitionKey);

        // Set initial values on the test item
        createCommand.Item.PublicMessage = "Public Message #1";
        createCommand.Item.PrivateMessage = "Private Message #1";
        createCommand.Item.OptionalMessage = "Optional Message #1";

        // Save the command and capture the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        // Get the event
        var eventId = $"EVENT^{id}^00000001";
        var key = new Dictionary<string, DynamoDBEntry>
        {
            { "partitionKey", partitionKey },
            { "id", eventId }
        };

        var document = await _eventTable.GetItemAsync(key, default);

        Assert.That(document, Is.Not.Null);
        Assert.That(document.ContainsKey("expireAt"), Is.False);
    }
}
