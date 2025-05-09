using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Base class for SQL Command Provider tests containing shared functionality.
/// </summary>
/// <remarks>
/// This class provides common infrastructure for testing SQL command providers, including:
/// - Shared configuration loading
/// - Connection string building
/// - Token credential management
/// - Test cleanup logic
/// </remarks>
public abstract class SqlCommandProviderTestBase : CommandProviderTests
{
    /// <summary>
    /// The scope for the Azure token credential.
    /// </summary>
    protected readonly string _scope = "https://database.windows.net/.default";

    /// <summary>
    /// The connection string used to connect to the SQL server.
    /// </summary>
    protected string _connectionString = null!;

    /// <summary>
    /// The data source or server name for the SQL server.
    /// </summary>
    /// <example>sqlcommandprovider-tests.database.windows.net</example>
    protected string _dataSource = null!;

    /// <summary>
    /// The initial catalog or database name for the SQL server.
    /// </summary>
    /// <example>trelnex-core-data-tests</example>
    protected string _initialCatalog = null!;

    /// <summary>
    /// The service configuration containing application settings like name, version, and description.
    /// </summary>
    /// <remarks>
    /// This configuration is loaded from the ServiceConfiguration section in appsettings.json.
    /// </remarks>
    protected ServiceConfiguration _serviceConfiguration = null!;

    /// <summary>
    /// The name of the table used for testing.
    /// </summary>
    protected string _tableName = null!;

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
        // Example: "sqlcommandprovider-tests.database.windows.net"
        _dataSource = configuration
            .GetSection("SqlCommandProviders:DataSource")
            .Value!;

        // Get the initial catalog from the configuration.
        // Example: "trelnex-core-data-tests"
        _initialCatalog = configuration
            .GetSection("SqlCommandProviders:InitialCatalog")
            .Value!;

        // Get the table name from the configuration.
        // Example: "test-items"
        _tableName = configuration
            .GetSection("SqlCommandProviders:Tables:0:TableName")
            .Value!;

        // Create the SQL connection string.
        var scsBuilder = new SqlConnectionStringBuilder()
        {
            ApplicationName = _serviceConfiguration.FullName,
            DataSource = _dataSource,
            InitialCatalog = _initialCatalog,
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
        // Establish a SQL connection using token authentication.
        using var sqlConnection = new SqlConnection(_connectionString);

        var tokenRequestContext = new TokenRequestContext([ _scope ]);
        sqlConnection.AccessToken = _tokenCredential.GetToken(tokenRequestContext, default).Token;

        sqlConnection.Open();

        // Define the SQL command to delete all rows from the main table and its events table.
        var cmdText = $"DELETE FROM [{_tableName}-events]; DELETE FROM [{_tableName}];";
        var sqlCommand = new SqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
    }
}
