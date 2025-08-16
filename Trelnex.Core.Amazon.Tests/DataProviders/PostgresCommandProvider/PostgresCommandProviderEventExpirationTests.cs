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
public class PostgresDataProviderEventExpirationTests : PostgresDataProviderEventTestBase
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
            TableNames: [_expirationTableName]
        );

        // Create the PostgresDataProviderFactory.
        var factory = await PostgresDataProviderFactory.Create(
            _serviceConfiguration,
            postgresClientOptions);

        // Create the data provider instance.
        _dataProvider = factory.Create<ITestItem, TestItem>(
            tableName: _expirationTableName,
            typeName: "expiration-test-item",
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            eventTimeToLive: 2);
    }

    [Test]
    [Description("Tests PostgresDataProvider sets expireAtDateTimeOffset correctly")]
    public async Task PostgresDataProviderr_WithExpiration()
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
        using var reader = await GetReader(sqlConnection, id, partitionKey, _expirationTableName);

        Assert.That(reader.Read(), Is.True);

        var expireAtDateTimeOffset = created.Item.CreatedDateTimeOffset.AddSeconds(2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That((DateTimeOffset)reader.GetDateTime(0), Is.EqualTo(expireAtDateTimeOffset));
        }
    }
}
