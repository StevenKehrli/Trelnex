using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Tests for the SqlDataProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="SqlDataProviderTestBase"/> to leverage the shared
/// test infrastructure and from <see cref="DataProviderTests"/> (indirectly) to leverage
/// the extensive test suite defined in that base class.
///
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual SQL Server instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
// [Ignore("Requires a SQL server.")]
[Category("SqlDataProvider")]
public class SqlDataProviderTests : SqlDataProviderTestBase
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
            TableNames: [_tableName]
        );

        // Create the SqlDataProviderFactory.
        var factory = await SqlDataProviderFactory.Create(
            _serviceConfiguration,
            sqlClientOptions);

        // Create the data provider instance.
        _dataProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    [Test]
    [Description("Tests SqlDataProvider with an optional message and without encryption to ensure data is properly encrypted and decrypted.")]
    public async Task SqlDataProvider_OptionalMessage_WithoutEncryption()
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
        using var reader = await GetReader(sqlConnection, id, partitionKey);

        Assert.That(reader.Read(), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(reader["privateMessage"], Is.EqualTo("Private Message #1"));
            Assert.That(reader["optionalMessage"], Is.EqualTo("Optional Message #1"));
        });
    }

    [Test]
    [Description("Tests SqlDataProvider without encryption to ensure data is properly encrypted and decrypted.")]
    public async Task SqlDataProvider_WithoutEncryption()
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
        using var reader = await GetReader(sqlConnection, id, partitionKey);

        Assert.That(reader.Read(), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(reader["privateMessage"], Is.EqualTo("Private Message #1"));
            Assert.That(reader.IsDBNull(1), Is.True);
        });
    }
}
