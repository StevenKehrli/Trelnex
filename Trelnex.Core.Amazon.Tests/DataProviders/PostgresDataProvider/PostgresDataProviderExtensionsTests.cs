using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Serilog;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

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
public class PostgresDataProviderExtensionsTests : PostgresDataProviderTestBase
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
                options => options.Add(
                    typeName: "test-item",
                    itemValidator: TestItem.Validator,
                    commandOperations: CommandOperations.All));

        var serviceProvider = services.BuildServiceProvider();

        // Get the data provider from the DI container.
        _dataProvider = serviceProvider.GetRequiredService<IDataProvider<TestItem>>();
    }

    /// <summary>
    /// Tests that registering the same type with the PostgresDataProvider twice results in an exception.
    /// </summary>
    [Test]
    [Description("Tests that registering the same type with the PostgresDataProvider twice results in an exception.")]
    public void PostgresDataProvider_AlreadyRegistered()
    {
        // Create the service collection.
        var services = new ServiceCollection();

        // Initialize shared resources from configuration
        var configuration = TestSetup();

        services.AddSingleton(_serviceConfiguration);

        // Configure Serilog
        var bootstrapLogger = services.AddSerilog(
            configuration,
            _serviceConfiguration);

        // Attempt to register the same type twice, which should throw an InvalidOperationException.
        Assert.Throws<InvalidOperationException>(() =>
        {
            services.AddPostgresDataProviders(
                configuration,
                bootstrapLogger,
                options => options
                    .Add(
                        typeName: "test-item",
                        itemValidator: TestItem.Validator,
                        commandOperations: CommandOperations.All)
                    .Add(
                        typeName: "test-item",
                        itemValidator: TestItem.Validator,
                        commandOperations: CommandOperations.All));
        });
    }

    [Test]
    [Description("Tests PostgresDataProvider with an optional message and without encryption to ensure data is properly encrypted and decrypted.")]
    public async Task PostgresDataProvider_OptionalMessage_WithoutEncryption()
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
            partitionKey: partitionKey,
            tableName: _itemTableName);

        Assert.That(reader.Read(), Is.True);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader["privateMessage"], Is.EqualTo("Private Message #1"));
            Assert.That(reader["optionalMessage"], Is.EqualTo("Optional Message #1"));
        }
    }

    [Test]
    [Description("Tests PostgresDataProvider without encryption to ensure data is properly encrypted and decrypted.")]
    public async Task PostgresDataProvider_WithoutEncryption()
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
            partitionKey: partitionKey,
            tableName: _itemTableName);

        Assert.That(reader.Read(), Is.True);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader["privateMessage"], Is.EqualTo("Private Message #1"));
            Assert.That(reader.IsDBNull(1), Is.True);
        }
    }
}
