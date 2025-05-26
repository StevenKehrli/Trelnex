using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Azure.Identity;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Encryption;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the extension methods used to register and configure CosmosCommandProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CosmosCommandProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="CommandProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This class focuses on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual CosmosDB instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
// [Ignore("Requires a CosmosDB instance.")]
[Category("CosmosCommandProvider")]
public class EncryptedCosmosCommandProviderExtensionsTests : CosmosCommandProviderTestBase
{
    /// <summary>
    /// Sets up the CosmosCommandProvider for testing using the dependency injection approach.
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

        // Add Azure Identity and Cosmos Command Providers to the service collection.
        services
            .AddAzureIdentity(
                configuration,
                bootstrapLogger)
            .AddCosmosCommandProviders(
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
    [Description("Tests CosmosCommandProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task CosmosCommandProvider_WithEncryption()
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

        // Get the item
        var item = await _encryptedContainer.ReadItemAsync<ValidateTestItem>(
            id: id,
            partitionKey: new Microsoft.Azure.Cosmos.PartitionKey(partitionKey),
            cancellationToken: default);

        Assert.That(item, Is.Not.Null);

        // Decrypt the private message
        var privateMessageEncryptedBytes = Convert.FromBase64String(item.Resource.PrivateMessage);
        var privateMessageBytes = encryptionService.Decrypt(privateMessageEncryptedBytes);
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
