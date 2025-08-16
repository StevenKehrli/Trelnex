using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

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
[Ignore("Requires a SQL server.")]
[Category("SqlDataProvider")]
public class EncryptedSqlDataProviderTests : SqlDataProviderTestBase
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
            TableNames: [_encryptedTableName]
        );

        // Create the SqlDataProviderFactory.
        var factory = await SqlDataProviderFactory.Create(
            _serviceConfiguration,
            sqlClientOptions);

        // Create the data provider instance.
        _dataProvider = factory.Create<ITestItem, TestItem>(
            tableName: _encryptedTableName,
            typeName: "encrypted-test-item",
            itemValidator: TestItem.Validator,
            commandOperations: CommandOperations.All,
            blockCipherService: _blockCipherService);
    }

    [Test]
    [Description("Tests SqlDataProvider with an optional message and encryption to ensure data is properly encrypted and decrypted.")]
    public async Task SqlDataProvider_OptionalMessage_WithEncryption()
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
        using var reader = await GetReader(sqlConnection, id, partitionKey, _encryptedTableName);

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
    [Description("Tests SqlDataProvider with encryption to ensure data is properly encrypted and decrypted.")]
    public async Task SqlDataProvider_WithEncryption()
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
        using var reader = await GetReader(sqlConnection, id, partitionKey, _encryptedTableName);

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
