using System.Text.Json;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the extension methods used to register and configure DynamoDataProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// Inherits from <see cref="DynamoDataProviderTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live DynamoDB instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoDataProvider")]
public class EncryptedDynamoDataProviderExtensionsTests : DynamoDataProviderTestBase
{
    /// <summary>
    /// Sets up the DynamoDataProvider for testing using the dependency injection approach.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Initialize shared resources from configuration
        var configuration = TestSetup();

        services.AddSingleton(_serviceConfiguration);

        // Create a credential provider for AWS services.
        var credentialProvider = new CredentialProvider();
        var awsCredentials = credentialProvider.GetCredential();

        services.AddCredentialProvider(credentialProvider);

        // Configure Serilog
        var bootstrapLogger = services.AddSerilog(
            configuration,
            _serviceConfiguration);

        // Add DynamoDataProviders to the service collection.
        services
            .AddDynamoDataProviders(
                configuration,
                bootstrapLogger,
                options => options.Add(
                    typeName: "encrypted-test-item",
                    itemValidator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider from the DI container.
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<TestItem>>();
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

        // Decrypt the optional message
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
