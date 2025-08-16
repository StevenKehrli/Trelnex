using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for the SqlDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// Inherits from <see cref="SqlDataProviderEventTestBase"/> to utilize shared test setup and infrastructure.
/// These tests require a live SQL Server instance and are not suitable for CI/CD environments without proper infrastructure.
/// </remarks>
[Ignore("Requires a SQL server.")]
[Category("SqlDataProvider")]
public class SqlDataProviderEventExpirationTests : SqlDataProviderEventTestBase
{
    /// <summary>
    /// Sets up the SqlDataProvider for testing using the direct factory instantiation approach.
    /// </summary>
    [OneTimeSetUp]
    public async Task TestFixtureSetup()
    {
        // Initialize shared resources from configuration
        TestSetup();

        // Configure the SQL client options.
        var sqlClientOptions = new SqlClientOptions(
            TokenCredential: _tokenCredential,
            Scope: _scope,
            DataSource: _dataSource,
            InitialCatalog: _initialCatalog,
            TableNames: [_expirationTableName]
        );

        // Create the SqlDataProviderFactory.
        var factory = await SqlDataProviderFactory.Create(
            _serviceConfiguration,
            sqlClientOptions);

        // Create the data provider instance.
        _dataProvider = factory.Create<ITestItem, TestItem>(
            tableName: _expirationTableName,
            typeName: "expiration-test-item",
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            eventTimeToLive: 2);
    }

    [Test]
    [Description("Tests SqlDataProvider sets expireAtDateTimeOffset correctly")]
    public async Task SqlDataProvider_WithExpiration()
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
            Assert.That(reader["expireAtDateTimeOffset"], Is.EqualTo(expireAtDateTimeOffset));
        }
    }
}
