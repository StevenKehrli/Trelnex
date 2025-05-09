using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Azure.CommandProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Azure.Tests.CommandProviders;

/// <summary>
/// Tests for the SqlCommandProvider implementation using direct factory instantiation.
/// </summary>
/// <remarks>
/// This class inherits from <see cref="CommandProviderTests"/> to leverage the extensive test suite
/// defined in the base class. The base class implements a comprehensive set of tests for command provider
/// functionality including:
/// <list type="bullet">
/// <item>Batch command operations (create, update, delete with success and failure scenarios)</item>
/// <item>Create command operations (with success and conflict handling)</item>
/// <item>Delete command operations (with success and precondition failure handling)</item>
/// <item>Query command operations (with various filters, ordering, paging)</item>
/// <item>Read command operations</item>
/// <item>Update command operations (with success and precondition failure handling)</item>
/// </list>
///
/// By inheriting from CommandProviderTests, this class runs all those tests against the SqlCommandProvider
/// implementation specifically, using direct factory instantiation rather than dependency injection.
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual SQL Server instance
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a SQL server.")]
[Category("SqlCommandProvider")]
public class SqlCommandProviderTests : CommandProviderTests
{
    /// <summary>
    /// The scope for the Azure token credential.
    /// </summary>
    private readonly string _scope = "https://database.windows.net/.default";

    /// <summary>
    /// The connection string used to connect to the SQL server.
    /// </summary>
    private string _connectionString = null!;

    /// <summary>
    /// The name of the table used for testing.
    /// </summary>
    private string _tableName = null!;

    /// <summary>
    /// The token credential used to authenticate with Azure.
    /// </summary>
    private TokenCredential _tokenCredential = null!;

    /// <summary>
    /// Sets up the SqlCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It uses direct factory instantiation,
    /// which tests the core functionality without the dependency injection layer.
    ///
    /// The setup process involves:
    /// <list type="number">
    /// <item>Loading configuration from appsettings.json</item>
    /// <item>Configuring SQL connection details</item>
    /// <item>Creating SqlClientOptions with the necessary parameters</item>
    /// <item>Creating the SqlCommandProviderFactory</item>
    /// <item>Using the factory to create a specific command provider instance</item>
    /// </list>
    /// </remarks>
    [OneTimeSetUp]
    public void TestFixtureSetup()
    {
        // Create the test configuration.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.User.json", optional: true, reloadOnChange: true)
            .Build();

        // Get the service configuration from the configuration.
        var serviceConfiguration = configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()!;

        // Get the data source from the configuration.
        // Example: "sqlcommandprovider-tests.database.windows.net"
        var dataSource = configuration
            .GetSection("SqlCommandProviders:DataSource")
            .Value!;

        // Get the initial catalog from the configuration.
        // Example: "trelnex-core-data-tests"
        var initialCatalog = configuration
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
            DataSource = dataSource,
            InitialCatalog = initialCatalog,
            Encrypt = true,
        };

        _connectionString = scsBuilder.ConnectionString;

        // Create the command provider using direct factory instantiation.
        // Use DefaultAzureCredential for authentication.
        _tokenCredential = new DefaultAzureCredential();

        // Configure the SQL client options.
        var sqlClientOptions = new SqlClientOptions(
            TokenCredential: _tokenCredential,
            Scope: _scope,
            DataSource: dataSource,
            InitialCatalog: initialCatalog,
            TableNames: [ _tableName ]
        );

        // Create the SqlCommandProviderFactory.
        var factory = SqlCommandProviderFactory.Create(
            serviceConfiguration,
            sqlClientOptions);

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    /// <summary>
    /// Cleans up the SQL tables after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all data from the SQL tables
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    ///
    /// The cleanup process involves:
    /// <list type="number">
    /// <item>Opening a SQL connection using token authentication</item>
    /// <item>Executing DELETE statements for both the main table and its events table</item>
    /// </list>
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
