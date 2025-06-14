using System.Text.Json.Serialization;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for the CosmosDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CosmosDataProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="DataProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a CosmosDB instance.")]
[Category("CosmosDataProvider")]
public class EncryptedCosmosDataProviderTests : CosmosDataProviderTestBase
{
    private EncryptionService _encryptionService = null!;

    /// <summary>
    /// Sets up the CosmosDataProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create the data provider using direct factory instantiation.
        var cosmosClientOptions = new CosmosClientOptions(
            TokenCredential: _tokenCredential,
            AccountEndpoint: _endpointUri,
            DatabaseId: _databaseId,
            ContainerIds: [ _encryptedContainerId ]
        );

        // Create the CosmosDataProviderFactory.
        var factory = await CosmosDataProviderFactory.Create(
            cosmosClientOptions);

        _encryptionService = EncryptionService.Create(Guid.NewGuid().ToString());

        // Create the data provider instance.
        _dataProvider = factory.Create<ITestItem, TestItem>(
            _encryptedContainerId,
            "encrypted-test-item",
            TestItem.Validator,
            CommandOperations.All,
            _encryptionService);
    }

    [Test]
    [Description("Tests CosmosDataProvider with an optional message and encryption to ensure data is properly encrypted and decrypted.")]
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
            _encryptionService);

        // Decrypt the optional message
        Assert.That(item.Resource.OptionalMessage, Is.Not.Null);

        var optionalMessage = EncryptedJsonService.DecryptFromBase64<string>(
            item.Resource.OptionalMessage,
            _encryptionService);

        Assert.Multiple(() =>
        {
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(optionalMessage, Is.EqualTo("Optional Message #1"));
        });
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
            _encryptionService);

        Assert.Multiple(() =>
        {
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(item.Resource.OptionalMessage, Is.Null);
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
