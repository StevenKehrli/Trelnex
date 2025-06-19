using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for the extension methods used to register and configure CosmosDataProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CosmosDataProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="DataProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This class focuses on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosDataProvider")]
public class EncryptedCosmosDataProviderExtensionsTests : CosmosDataProviderTestBase
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

        // Add Azure Identity and CosmosDataProviders to the service collection.
        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddCosmosDataProviders(
                configuration,
                bootstrapLogger,
                options => options.Add<ITestItem, TestItem>(
                    typeName: "encrypted-test-item",
                    validator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider from the DI container.
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<ITestItem>>();
    }

    [Test]
    [Description("Tests CosmosDataProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task CosmosDataProvider_WithEncryption()
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

        // Get the item
        var item = await _encryptedContainer.ReadItemAsync<ValidateTestItem>(
            id: id,
            partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.Resource.PrivateMessage,
            _blockCipherService);

        // Decrypt the optional message
        Assert.That(item.Resource.OptionalMessage, Is.Not.Null);

        var optionalMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.Resource.OptionalMessage,
            _blockCipherService);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.Resource.PrivateMessage, Is.Not.EqualTo("Private Message #1"));
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.Resource.OptionalMessage, Is.Not.EqualTo("Optional Message #1"));
            Assert.That(optionalMessage, Is.EqualTo("Optional Message #1"));
        }
    }

    [Test]
    [Description("Tests CosmosDataProvider with optional message and encryption to ensure data is properly encrypted and decrypted.")]
    public async Task CosmosDataProvider_OptionalMessage_WithEncryption()
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

        // Get the item
        var item = await _encryptedContainer.ReadItemAsync<ValidateTestItem>(
            id: id,
            partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.Resource.PrivateMessage,
            _blockCipherService);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.Resource.PrivateMessage, Is.Not.EqualTo("Private Message #1"));
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.Resource.OptionalMessage, Is.Null);
        }
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
