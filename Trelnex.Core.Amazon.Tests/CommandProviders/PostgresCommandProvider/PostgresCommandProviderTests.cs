using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Trelnex.Core.Amazon.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

/// <summary>
/// Tests for the PostgresCommandProvider implementation using direct factory instantiation.
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
/// By inheriting from CommandProviderTests, this class runs all those tests against the PostgresCommandProvider
/// implementation specifically, using direct factory instantiation rather than dependency injection.
/// This approach tests the factory pattern and provider implementation directly.
///
/// This test class is marked with <see cref="IgnoreAttribute"/> as it requires an actual PostgreSQL server
/// to run, making it unsuitable for automated CI/CD pipelines without proper infrastructure setup.
/// </remarks>
[Ignore("Requires a Postgres server.")]
[Category("PostgresCommandProvider")]
public class PostgresCommandProviderTests : CommandProviderTests
{
    /// <summary>
    /// The connection string used to connect to the PostgreSQL server.
    /// </summary>
    private string _connectionString = null!;

    /// <summary>
    /// The name of the table used for testing.
    /// </summary>
    private string _tableName = null!;

    /// <summary>
    /// Sets up the PostgresCommandProvider for testing using the direct factory instantiation approach.
    /// </summary>
    /// <remarks>
    /// This method initializes the command provider that will be tested by all the test methods
    /// inherited from <see cref="CommandProviderTests"/>. It uses direct factory instantiation,
    /// which tests the core functionality without the dependency injection layer.
    ///
    /// The setup process involves:
    /// <list type="number">
    /// <item>Loading configuration from appsettings.json</item>
    /// <item>Retrieving AWS credentials via the default identity resolver</item>
    /// <item>Generating an RDS authentication token</item>
    /// <item>Creating a connection string for test cleanup</item>
    /// <item>Creating PostgresClientOptions with the necessary parameters</item>
    /// <item>Creating the PostgresCommandProviderFactory</item>
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

        // Get the region from the configuration.
        // Example: "us-west-2"
        var region = configuration
            .GetSection("PostgresCommandProviders:Region")
            .Value!;

        // Get the host from the configuration.
        // Example: "postgrescommandprovider-tests.us-west-2.rds.amazonaws.com"
        var host = configuration
            .GetSection("PostgresCommandProviders:Host")
            .Value!;

        // Get the port from the configuration.
        // Example: 5432
        var port = int.Parse(
            configuration
                .GetSection("PostgresCommandProviders:Port")
                .Value!);

        // Get the database from the configuration.
        // Example: "trelnex-core-data-tests"
        var database = configuration
            .GetSection("PostgresCommandProviders:Database")
            .Value!;

        // Get the database user from the configuration.
        // Example: "admin"
        var dbUser = configuration
            .GetSection("PostgresCommandProviders:DbUser")
            .Value!;

        // Get the table name from the configuration.
        // Example: "test-items"
        _tableName = configuration
            .GetSection("PostgresCommandProviders:Tables:0:TableName")
            .Value!;

        // Create a postgres client for cleanup.
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        var regionEndpoint = RegionEndpoint.GetBySystemName(region);

        // Generate an RDS authentication token.
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: awsCredentials,
            region: regionEndpoint,
            hostname: host,
            port: port,
            dbUser: dbUser);

        // Build the connection string for cleanup operations.
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = host,
            Port = port,
            Database = database,
            Username = dbUser,
            Password = pwd,
            SslMode = SslMode.Require
        };

        _connectionString = csb.ConnectionString;

        // Create the command provider using direct factory instantiation.
        var postgresClientOptions = new PostgresClientOptions(
            AWSCredentials: awsCredentials,
            Region: region,
            Host: host,
            Port: port,
            Database: database,
            DbUser: dbUser,
            TableNames: [ _tableName ]
        );

        // Create the PostgresCommandProviderFactory.
        var factory = PostgresCommandProviderFactory.Create(
            serviceConfiguration,
            postgresClientOptions);

        // Create the command provider instance.
        _commandProvider = factory.Create<ITestItem, TestItem>(
            _tableName,
            "test-item",
            TestItem.Validator,
            CommandOperations.All);
    }

    /// <summary>
    /// Cleans up the PostgreSQL tables after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all data from the PostgreSQL tables
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    ///
    /// The cleanup process involves:
    /// <list type="number">
    /// <item>Opening a SQL connection using the connection string</item>
    /// <item>Executing DELETE statements for both the main table and its events table</item>
    /// </list>
    /// </remarks>
    [TearDown]
    public void TestCleanup()
    {
        // Establish a SQL connection using the connection string.
        using var sqlConnection = new NpgsqlConnection(_connectionString);

        sqlConnection.Open();

        // Define the SQL command to delete all rows from the main table and its events table.
        var cmdText = $"DELETE FROM \"{_tableName}-events\"; DELETE FROM \"{_tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
    }
}
