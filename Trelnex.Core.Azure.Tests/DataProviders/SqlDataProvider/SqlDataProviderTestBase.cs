using System.Data.Common;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.DataProviders;

/// <summary>
/// Base class for SqlDataProvider tests containing shared functionality.
/// </summary>
/// <remarks>
/// This class provides common infrastructure for testing SQL data providers, including:
/// - Shared configuration loading
/// - Connection string building
/// - Token credential management
/// - Test cleanup logic
/// </remarks>
public abstract class SqlDataProviderTestBase : DataProviderTests
{
    /// <summary>
    /// The scope for the Azure token credential.
    /// </summary>
    protected readonly string _scope = "https://database.windows.net/.default";

    /// <summary>
    /// The block cipher service used for encrypting and decrypting test data.
    /// </summary>
    protected IBlockCipherService _blockCipherService = null!;

    /// <summary>
    /// The connection string used to connect to the SQL server.
    /// </summary>
    protected string _connectionString = null!;

    /// <summary>
    /// The name of the event table used for testing.
    /// </summary>
    protected string _eventTableName = null!;

    /// <summary>
    /// The name of the item table used for testing.
    /// </summary>
    protected string _itemTableName = null!;

    /// <summary>
    /// The service configuration containing application settings like name, version, and description.
    /// </summary>
    /// <remarks>
    /// This configuration is loaded from the ServiceConfiguration section in appsettings.json.
    /// </remarks>
    protected ServiceConfiguration _serviceConfiguration = null!;

    /// <summary>
    /// The token credential used to authenticate with Azure.
    /// </summary>
    protected TokenCredential _tokenCredential = null!;

    protected IConfiguration TestSetup()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the service configuration from the configuration.
        _serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        // Get the data source from the configuration.
        // Example: "sqldataprovider-tests.database.windows.net"
        var dataSource = configuration
            .GetSection("Azure.SqlDataProviders:DataSource")
            .Get<string>();

        // Get the initial catalog from the configuration.
        // Example: "trelnex-core-data-tests"
        var initialCatalog = configuration
            .GetSection("Azure.SqlDataProviders:InitialCatalog")
            .Get<string>();

        // Get the item table name from the configuration.
        // Example: "test-items"
        var testItemItemTableName = configuration
            .GetSection("Azure.SqlDataProviders:Tables:test-item:ItemTableName")
            .Get<string>();

        // Get the item table name from the configuration.
        // Example: "test-items-events"
        var testItemEventTableName = configuration
            .GetSection("Azure.SqlDataProviders:Tables:test-item:EventTableName")
            .Get<string>();

        // Get the encrypted item table name from the configuration.
        // Example: "test-items"
        var encryptedTestItemItemTableName = configuration
            .GetSection("Azure.SqlDataProviders:Tables:encrypted-test-item:ItemTableName")
            .Get<string>();

        // Get the encrypted event table name from the configuration.
        // Example: "test-items-events"
        var encryptedTestItemEventTableName = configuration
            .GetSection("Azure.SqlDataProviders:Tables:encrypted-test-item:EventTableName")
            .Get<string>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(encryptedTestItemItemTableName, Is.EqualTo(testItemItemTableName));
            Assert.That(encryptedTestItemEventTableName, Is.EqualTo(testItemEventTableName));
        }

        _itemTableName = testItemItemTableName!;
        _eventTableName = testItemEventTableName!;

        // Create the block cipher service from configuration using the factory pattern.
        // This deserializes the algorithm type and settings, then creates the appropriate service.
        _blockCipherService = configuration
            .GetSection("Azure.SqlDataProviders:Tables:encrypted-test-item")
            .CreateBlockCipherService()!;

        // Create the SQL connection string.
        var scsBuilder = new SqlConnectionStringBuilder()
        {
            ApplicationName = _serviceConfiguration.FullName,
            DataSource = dataSource,
            InitialCatalog = initialCatalog,
            Encrypt = true,
        };

        _connectionString = scsBuilder.ConnectionString;

        // Create the token credential.
        _tokenCredential = new DefaultAzureCredential();

        return configuration;
    }

    /// <summary>
    /// Cleans up the SQL tables after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all data from the SQL tables
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public void TestCleanup()
    {
        TableCleanup(_eventTableName);
        TableCleanup(_itemTableName);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        LinqToDB.Mapping.MappingSchema.ClearCache();
    }

    protected SqlConnection GetConnection()
    {
        // Establish a SQL connection using token authentication.
        var sqlConnection = new SqlConnection(_connectionString);

        var tokenRequestContext = new TokenRequestContext([_scope]);
        sqlConnection.AccessToken = _tokenCredential.GetToken(tokenRequestContext, default).Token;

        sqlConnection.Open();

        return sqlConnection;
    }

    private void TableCleanup(
        string tableName)
    {
        // Establish a SQL connection using token authentication.
        using var sqlConnection = GetConnection();

        // Define the SQL command to delete all rows from the table
        var cmdText = $"DELETE FROM [{tableName}];";
        var sqlCommand = new SqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
    }

    protected async Task<SqlDataReader> GetReader(
        SqlConnection sqlConnection,
        string id,
        string partitionKey,
        string tableName)
    {
        // Define the SQL command to get the private message and optional message.
        var cmdText = $"SELECT [privateMessage], [optionalMessage] FROM [{tableName}] WHERE [id] = @id AND [partitionKey] = @partitionKey;";

        var sqlCommand = new SqlCommand(cmdText, sqlConnection);
        sqlCommand.Parameters.AddWithValue("@id", id);
        sqlCommand.Parameters.AddWithValue("@partitionKey", partitionKey);

        return await sqlCommand.ExecuteReaderAsync();
    }

    protected void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        // Only process SqlConnection connections
        if (dbConnection is not SqlConnection connection) return;

        // Get Azure authentication token for SQL Server
        var tokenRequestContext = new TokenRequestContext([_scope]);
        connection.AccessToken = _tokenCredential.GetToken(tokenRequestContext, default).Token;
    }
}
