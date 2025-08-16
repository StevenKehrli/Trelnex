using System.Text.Json;
using Amazon.DynamoDBv2.DocumentModel;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the DynamoDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// Inherits from <see cref="DynamoDataProviderTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live DynamoDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoDataProvider")]
public class EncryptedDynamoDataProviderTests : DynamoDataProviderTestBase
{
    /// <summary>
    /// Sets up the <see cref="DynamoDataProvider"/> with encryption for testing using the direct factory instantiation approach.
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
            TableNames: [_encryptedTableName]
        );

        var factory = await DynamoDataProviderFactory.Create(
            dynamoClientOptions);

        _dataProvider = factory.Create(
            typeName: "encrypted-test-item",
            tableName: _encryptedTableName,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            blockCipherService: _blockCipherService);
    }

    [Test]
    [Description("Tests DynamoDataProvider with optional message and encryption to ensure data is properly encrypted and decrypted.")]
    public async Task DynamoDataProvider_OptionalMessage_WithEncryption()
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

        var document = await _encryptedTable.GetItemAsync(key, default);

        // Convert to json
        var json = document.ToJson();

        // Deserialize the item
        var item = JsonSerializer.Deserialize<TestItem>(json);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.PrivateMessage,
            _blockCipherService);

        Assert.That(item.OptionalMessage, Is.Not.Null);

        var optionalMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.OptionalMessage,
            _blockCipherService);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.PrivateMessage, Is.Not.EqualTo("Private Message #1"));
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.OptionalMessage, Is.Not.EqualTo("Optional Message #1"));
            Assert.That(optionalMessage, Is.EqualTo("Optional Message #1"));
        }
    }

    [Test]
    [Description("Tests DynamoDataProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task DynamoDataProvider_WithEncryption()
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

        var document = await _encryptedTable.GetItemAsync(key, default);

        // Convert to json
        var json = document.ToJson();

        // Deserialize the item
        var item = JsonSerializer.Deserialize<TestItem>(json);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.PrivateMessage,
            _blockCipherService);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.PrivateMessage, Is.Not.EqualTo("Private Message #1"));
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.OptionalMessage, Is.Null);
        }
    }
}
