using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DocumentModel;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the DynamoDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="DynamoDataProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="DataProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual DynamoDB table
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoDataProvider")]
public class DynamoDataProviderTests : DynamoDataProviderTestBase
{
    /// <summary>
    /// Sets up the DynamoDataProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create the data provider using direct factory instantiation.
        var dynamoClientOptions = new DynamoClientOptions(
            AWSCredentials: _awsCredentials,
            Region: _region,
            TableNames: [_tableName]
        );

        var factory = await DynamoDataProviderFactory.Create(
            dynamoClientOptions);

        _dataProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    [Test]
    [Description("Tests DynamoDataProvider with optional message and without encryption to ensure data is properly stored and retrieved.")]
    public async Task DynamoDataProvider_OptionalMessage_WithoutEncryption()
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

        // Get the document
        var key = new Dictionary<string, DynamoDBEntry>
        {
            { "partitionKey", partitionKey },
            { "id", id }
        };

        var document = await _table.GetItemAsync(key, default);

        // Convert to json
        var json = document.ToJson();

        // Deserialize the item
        var item = JsonSerializer.Deserialize<ValidateTestItem>(json);

        Assert.That(item, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.PrivateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.OptionalMessage, Is.EqualTo("Optional Message #1"));
        };
    }

    [Test]
    [Description("Tests DynamoDataProvider without encryption to ensure data is properly stored and retrieved.")]
    public async Task DynamoDataProvider_WithoutEncryption()
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

        // Save the command and capture the result
        var created = await createCommand.SaveAsync(
            cancellationToken: default);

        Assert.That(created, Is.Not.Null);

        // Get the document
        var key = new Dictionary<string, DynamoDBEntry>
        {
            { "partitionKey", partitionKey },
            { "id", id }
        };

        var document = await _table.GetItemAsync(key, default);

        // Convert to json
        var json = document.ToJson();

        // Deserialize the item
        var item = JsonSerializer.Deserialize<ValidateTestItem>(json);

        Assert.That(item, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.PrivateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.OptionalMessage, Is.Null);
        };
    }

    private class ValidateTestItem : BaseItem, ITestItem, IBaseItem
    {
        [JsonPropertyName("publicMessage")]
        public string PublicMessage { get; set; } = null!;

        [JsonPropertyName("privateMessage")]
        public string PrivateMessage { get; set; } = null!;

        [JsonPropertyName("optionalMessage")]
        public string? OptionalMessage { get; set; }
    }
}
