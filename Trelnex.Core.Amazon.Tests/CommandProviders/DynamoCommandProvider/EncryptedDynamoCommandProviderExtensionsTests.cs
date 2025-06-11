using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

/// <summary>
/// Tests for the extension methods used to register and configure DynamoCommandProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="DynamoCommandProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This class focuses on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual DynamoDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a DynamoDB table.")]
[Category("DynamoCommandProvider")]
public class EncryptedDynamoCommandProviderExtensionsTests : DynamoCommandProviderTestBase
{
    /// <summary>
    /// Sets up the DynamoCommandProvider for testing using the dependency injection approach.
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

        // Add DynamoDB Command Providers to the service collection.
        services
            .AddDynamoCommandProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "encrypted-test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the command provider from the DI container.
        _commandProvider = serviceProvider.GetRequiredService<ICommandProvider<ITestItem>>();
    }

    [Test]
    [Description("Tests DynamoCommandProvider with optional message and encryption to ensure data is properly encrypted and decrypted.")]
    public async Task DynamoCommandProvider_OptionalMessage_WithEncryption()
    {
        var encryptionService = EncryptionService.Create(_encryptionSecret);

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
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
        var item = JsonSerializer.Deserialize<ValidateTestItem>(json);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.PrivateMessage,
            encryptionService);

        // Decrypt the optional message
        Assert.That(item.OptionalMessage, Is.Not.Null);

        var optionalMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.OptionalMessage,
            encryptionService);

        Assert.Multiple(() =>
        {
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(optionalMessage, Is.EqualTo("Optional Message #1"));
        });
    }

    [Test]
    [Description("Tests DynamoCommandProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task DynamoCommandProvider_WithEncryption()
    {
        var encryptionService = EncryptionService.Create(_encryptionSecret);

        var id = Guid.NewGuid().ToString();
        var partitionKey = Guid.NewGuid().ToString();

        // Create a command for creating a test item
        var createCommand = _commandProvider.Create(
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
        var item = JsonSerializer.Deserialize<ValidateTestItem>(json);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.PrivateMessage,
            encryptionService);

        Assert.Multiple(() =>
        {
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.OptionalMessage, Is.Null);
        });
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
