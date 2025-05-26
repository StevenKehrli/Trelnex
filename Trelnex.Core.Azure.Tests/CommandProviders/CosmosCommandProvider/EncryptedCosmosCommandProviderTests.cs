using System.Text;
using System.Text.Json.Serialization;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Encryption;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the CosmosCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CosmosCommandProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
// [Ignore("Requires a CosmosDB instance.")]
[Category("CosmosCommandProvider")]
public class EncryptedCosmosCommandProviderTests : CosmosCommandProviderTestBase
{
    private EncryptionService _encryptionService = null!;

    /// <summary>
    /// Sets up the CosmosCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create the command provider using direct factory instantiation.
        var cosmosClientOptions = new CosmosClientOptions(
            TokenCredential: _tokenCredential,
            AccountEndpoint: _endpointUri,
            DatabaseId: _databaseId,
            ContainerIds: [ _encryptedContainerId ]
        );

        // Create the CosmosCommandProviderFactory.
        var factory = await CosmosCommandProviderFactory.Create(
            cosmosClientOptions);

        _encryptionService = EncryptionService.Create(Guid.NewGuid().ToString());

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            _containerId,
            "encrypted-test-item",
            TestItem.Validator,
            CommandOperations.All,
            _encryptionService);
    }

    [Test]
    [Description("Tests CosmosCommandProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task CosmosCommandProvider_WithEncryption()
    {
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

        // Get the item
        var item = await _encryptedContainer.ReadItemAsync<ValidateTestItem>(
            id: id,
            partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessageEncryptedBytes = Convert.FromBase64String(item.Resource.PrivateMessage);
        var privateMessageBytes = _encryptionService.Decrypt(privateMessageEncryptedBytes);
        var privateMessage = Encoding.UTF8.GetString(privateMessageBytes);

        Assert.That(privateMessage, Is.EqualTo("\"Private Message #1\""));
    }

    private class ValidateTestItem : BaseItem, ITestItem, IBaseItem
    {
        [JsonPropertyName("publicMessage")]
        public string PublicMessage { get; set; } = null!;

        [JsonPropertyName("privateMessage")]
        public string PrivateMessage { get; set; } = null!;
    }
}
