using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the PostgresDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// Inherits from <see cref="PostgresDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live SQL Server instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresDataProvider")]
public class PostgresDataProviderEventPersistenceTests : PostgresDataProviderEventTestBase
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
            typeName: "expiration-test-item",
            itemTableName: _itemTableName,
            eventTableName: _eventTableName,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All);
    }

    [Test]
    [Description("Tests PostgresDataProvider sets expireAtDateTimeOffset correctly")]
    public async Task PostgresDataProviderr_WithoutExpiration()
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

        // Retrieve the expireAtDateTimeOffset using the helper method.
        using var sqlConnection = GetConnection();
        using var reader = await GetReader(
            sqlConnection: sqlConnection,
            id: id,
            partitionKey: partitionKey,
            tableName: _eventTableName);

        Assert.That(reader.Read(), Is.True);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(reader.IsDBNull(0), Is.True);
        }
    }
}
