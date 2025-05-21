using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data.Tests.CommandProviders;

namespace Trelnex.Core.Amazon.Tests.CommandProviders;

/// <summary>
/// Base class for PostgreSQL Command Provider tests containing shared functionality.
/// </summary>
/// <remarks>
/// This class provides common infrastructure for testing PostgreSQL command providers, including:
/// - Shared configuration loading
/// - Connection string building
/// - AWS credential management
/// - Test cleanup logic
/// </remarks>
public abstract class PostgresCommandProviderTestBase : CommandProviderTests
{
    /// <summary>
    /// The AWS credentials for RDS authentication.
    /// </summary>
    protected AWSCredentials _awsCredentials = null!;

    /// <summary>
    /// The connection string used to connect to the PostgreSQL server.
    /// </summary>
    protected string _connectionString = null!;

    /// <summary>
    /// The database name for the PostgreSQL server.
    /// </summary>
    /// <example>trelnex-core-data-tests</example>
    protected string _database = null!;

    /// <summary>
    /// The database user for the PostgreSQL server.
    /// </summary>
    /// <example>admin</example>
    protected string _dbUser = null!;

    /// <summary>
    /// The host or server name for the PostgreSQL server.
    /// </summary>
    /// <example>postgrescommandprovider-tests.us-west-2.rds.amazonaws.com</example>
    protected string _host = null!;

    /// <summary>
    /// The port for the PostgreSQL server.
    /// </summary>
    /// <example>5432</example>
    protected int _port;

    /// <summary>
    /// The region for AWS services.
    /// </summary>
    /// <example>us-west-2</example>
    protected RegionEndpoint _region = null!;

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
    /// Sets up the common test infrastructure for PostgreSQL command provider tests.
    /// </summary>
    /// <returns>The loaded configuration.</returns>
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

        // Get the host from the configuration.
        // Example: "instanceName.uniqueId.region.rds.amazonaws.com"
        _host = configuration
            .GetSection("Amazon.PostgresCommandProviders:Host")
            .Value!;

        // Get the region from the host.
        // Example: "us-west-2"
        var regionSystemName = _host.Split('.')[2];
        _region = RegionEndpoint.GetBySystemName(regionSystemName);

        // Get the port from the configuration.
        // Example: 5432
        _port = int.Parse(
            configuration
                .GetSection("Amazon.PostgresCommandProviders:Port")
                .Value!);

        // Get the database from the configuration.
        // Example: "trelnex-core-data-tests"
        _database = configuration
            .GetSection("Amazon.PostgresCommandProviders:Database")
            .Value!;

        // Get the database user from the configuration.
        // Example: "admin"
        _dbUser = configuration
            .GetSection("Amazon.PostgresCommandProviders:DbUser")
            .Value!;

        // Get the table name from the configuration.
        // Example: "test-items"
        _tableName = configuration
            .GetSection("Amazon.PostgresCommandProviders:Tables:0:TableName")
            .Value!;

        // Create AWS credentials
        _awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Generate an RDS authentication token.
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _awsCredentials,
            region: _region,
            hostname: _host,
            port: _port,
            dbUser: _dbUser);

        // Build the connection string.
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = _serviceConfiguration.FullName,
            Host = _host,
            Port = _port,
            Database = _database,
            Username = _dbUser,
            Password = pwd,
            SslMode = SslMode.Require
        };

        _connectionString = csb.ConnectionString;

        return configuration;
    }

    /// <summary>
    /// Cleans up the PostgreSQL tables after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all data from the PostgreSQL tables
    /// after each test runs. This prevents state from one test affecting subsequent tests.
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
