using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the extension methods used to register and configure PostgresDataProviders
/// in the dependency injection container.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="PostgresDataProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="DataProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This class focuses on testing the extension methods for DI registration rather than
/// direct factory instantiation. It also adds an additional test for duplicate registration handling.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual PostgreSQL server
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresDataProvider")]
public class EncryptedPostgresDataProviderExtensionsTests : PostgresDataProviderTestBase
{
    /// <summary>
    /// Sets up the PostgresDataProvider for testing using the dependency injection approach.
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

        // Add PostgreDataProviders to the service collection.
        services
            .AddPostgresDataProviders(
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
    [Description("Tests PostgresDataProvider with an optional message and encryption to ensure data is properly encrypted and decrypted.")]
    public async Task PostgresDataProvider_OptionalMessage_WithEncryption()
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

        // Retrieve the private and optional messages using the helper method.
        using var sqlConnection = GetConnection();

        using var reader = await GetReader(
            sqlConnection: sqlConnection,
            id: id,
            partitionKey: partitionKey);

        Assert.That(reader.Read(), Is.True);

        // Decrypt the private message
        var encryptedPrivateMessage = (reader["privateMessage"] as string)!;
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            encryptedPrivateMessage,
            _blockCipherService);

        // Decrypt the optional message
        var encryptedOptionalMessage = (reader["optionalMessage"] as string)!;
        var optionalMessage = EncryptedJsonService.DecryptFromBase64<string>(
            encryptedOptionalMessage,
            _blockCipherService);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encryptedPrivateMessage, Is.Not.EqualTo("Private Message #1"));
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(encryptedOptionalMessage, Is.Not.EqualTo("Optional Message #1"));
            Assert.That(optionalMessage, Is.EqualTo("Optional Message #1"));
        }
    }

    [Test]
    [Description("Tests PostgresDataProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task PostgresDataProvider_WithEncryption()
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

        // Retrieve the private and optional messages using the helper method.
        using var sqlConnection = GetConnection();

        using var reader = await GetReader(
            sqlConnection: sqlConnection,
            id: id,
            partitionKey: partitionKey);

        Assert.That(reader.Read(), Is.True);

        // Decrypt the private message
        var encryptedPrivateMessage = (reader["privateMessage"] as string)!;
        var privateMessage = EncryptedJsonService.DecryptFromBase64<string>(
            encryptedPrivateMessage,
            _blockCipherService);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encryptedPrivateMessage, Is.Not.EqualTo("Private Message #1"));
            Assert.That(privateMessage, Is.EqualTo("Private Message #1"));
            Assert.That(reader.IsDBNull(1), Is.True);
        }
    }
}
