using LinqToDB;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Tests for the PostgresDataProvider implementation using direct constructor instantiation.
/// </summary>
/// <remarks>
/// Inherits from <see cref="PostgresDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live PostgreSQL server instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresDataProvider")]
public class PostgresDataProviderEventExpirationTests : PostgresDataProviderEventTestBase
{
    /// <summary>
    /// Sets up the PostgresDataProvider for testing using direct constructor instantiation.
    /// </summary>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Create base DataOptions with PostgreSQL connection string
        var baseDataOptions = new DataOptions().UsePostgreSQL(_connectionString);

        // Create the data provider using DataOptionsBuilder and constructor
        var dataOptions = DataOptionsBuilder.Build<TestItem>(
            baseDataOptions: baseDataOptions,
            beforeConnectionOpened: BeforeConnectionOpened,
            itemTableName: _itemTableName,
            eventTableName: _eventTableName);

        _dataProvider = new PostgresDataProvider<TestItem>(
            typeName: "expiration-test-item",
            dataOptions: dataOptions,
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            eventPolicy: EventPolicy.AllChanges,
            eventTimeToLive: 2);
    }

    [Test]
    [Description("Tests PostgresDataProvider sets expireAtDateTimeOffset correctly")]
    public async Task PostgresDataProvider_WithExpiration()
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

        var expireAtDateTimeOffset = created.Item.CreatedDateTimeOffset.AddSeconds(2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((DateTimeOffset)reader.GetDateTime(0), Is.EqualTo(expireAtDateTimeOffset));
        }
    }
}
