using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the PostgresDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="PostgresDataProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="DataProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual PostgreSQL server
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresDataProvider")]
public class PostgresDataProviderTests : PostgresDataProviderTestBase
{
    /// <summary>
    /// Sets up the PostgresDataProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create the PostgresClientOptions.
        var postgresClientOptions = new PostgresClientOptions(
            AWSCredentials: _awsCredentials,
            Region: _region,
            Host: _host,
            Port: _port,
            Database: _database,
            DbUser: _dbUser,
            TableNames: [ _itemTableName, _eventTableName ]
        );

        // Create the PostgresDataProviderFactory.
        var factory = await PostgresDataProviderFactory.Create(
            _serviceConfiguration,
            postgresClientOptions);

        // Create the data provider instance.
        _dataProvider = factory.Create(
            typeName: "test-item",
            itemTableName: _itemTableName,
            eventTableName: _eventTableName,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All);
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
