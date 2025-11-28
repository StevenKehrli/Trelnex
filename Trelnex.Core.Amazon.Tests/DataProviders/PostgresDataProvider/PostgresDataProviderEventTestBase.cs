using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.DataProviders;

namespace Trelnex.Core.Amazon.Tests.DataProviders;

/// <summary>
/// Base class for PostgresDataProvider tests containing shared functionality.
/// </summary>
/// <remarks>
/// This class provides common infrastructure for testing PostgreSQL data providers, including:
/// - Shared configuration loading
/// - Connection string building
/// - AWS credential management
/// - Test cleanup logic
/// </remarks>
public abstract class PostgresDataProviderEventTestBase
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
    /// <example>postgresdataprovider-tests.us-west-2.rds.amazonaws.com</example>
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
    /// The name of the item table used for expiration testing.
    /// </summary>
    protected string _itemTableName = null!;

    /// <summary>
    /// The name of the event table used for expiration testing.
    /// </summary>
    protected string _eventTableName = null!;

    /// <summary>
    /// The data provider used for testing.
    /// </summary>
    protected IDataProvider<TestItem> _dataProvider = null!;

    /// <summary>
    /// Sets up the common test infrastructure for PostgreSQL data provider tests.
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
            .GetSection("Amazon.PostgresDataProviders:Host")
            .Get<string>()!;

        // Get the region from the host.
        // Example: "us-west-2"
        var regionSystemName = _host.Split('.')[2];
        _region = RegionEndpoint.GetBySystemName(regionSystemName);

        // Get the port from the configuration.
        // Example: 5432
        _port = configuration
            .GetSection("Amazon.PostgresDataProviders:Port")
            .Get<int?>() ?? 5432;

        // Get the database from the configuration.
        // Example: "trelnex-core-data-tests"
        _database = configuration
            .GetSection("Amazon.PostgresDataProviders:Database")
            .Get<string>()!;

        // Get the database user from the configuration.
        // Example: "admin"
        _dbUser = configuration
            .GetSection("Amazon.PostgresDataProviders:DbUser")
            .Get<string>()!;

        // Get the expiration item table name from the configuration.
        // Example: "test-items"
        var expirationTestItemItemTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:expiration-test-item:ItemTableName")
            .Get<string>()!;

        // Get the expiration event table name from the configuration.
        // Example: "test-items-events"
        var expirationTestItemEventTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:expiration-test-item:EventTableName")
            .Get<string>()!;

        // Get the persistence item table name from the configuration.
        // Example: "test-items"
        var persistanceTestItemItemTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:test-item:ItemTableName")
            .Get<string>()!;

        // Get the persistence event table name from the configuration.
        // Example: "test-items-events"
        var persistanceTestItemEventTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:test-item:EventTableName")
            .Get<string>()!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(persistanceTestItemItemTableName, Is.EqualTo(expirationTestItemItemTableName));
            Assert.That(persistanceTestItemEventTableName, Is.EqualTo(expirationTestItemEventTableName));
        }

        _itemTableName = expirationTestItemItemTableName;
        _eventTableName = expirationTestItemEventTableName;

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
        TableCleanup(_eventTableName);
        TableCleanup(_itemTableName);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        LinqToDB.Mapping.MappingSchema.ClearCache();
    }

    private void TableCleanup(
        string tableName)
    {
        // Establish a SQL connection using the connection string.
        using var sqlConnection = GetConnection();

        // Define the SQL command to delete all rows from the table.
        var cmdText = $"DELETE FROM \"{tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
    }

    protected void BeforeConnectionOpened(
        DbConnection dbConnection)
    {
        // Only process Npgsql connections
        if (dbConnection is not NpgsqlConnection connection) return;

        // Generate AWS IAM authentication token for PostgreSQL
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _awsCredentials,
            region: _region,
            hostname: _host,
            port: _port,
            dbUser: _dbUser);

        // Update connection string with generated authentication token
        var csb = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
        {
            Password = pwd,
            SslMode = SslMode.Require
        };

        connection.ConnectionString = csb.ConnectionString;
    }

    protected NpgsqlConnection GetConnection()
    {
        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: _awsCredentials,
            region: _region,
            hostname: _host,
            port: _port,
            dbUser: _dbUser);

        var csb = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Password = pwd,
            SslMode = SslMode.Require
        };

        // Establish a SQL connection using the connection string.
        var sqlConnection = new NpgsqlConnection(csb.ConnectionString);

        sqlConnection.Open();

        return sqlConnection;
    }

    protected async Task<NpgsqlDataReader> GetReader(
        NpgsqlConnection sqlConnection,
        string id,
        string partitionKey,
        string tableName)
    {
        // Define the SQL command to get the private message and optional message.
        var cmdText = $"SELECT \"expireAtDateTimeOffset\" FROM \"{tableName}\" WHERE \"id\" = @id AND \"partitionKey\" = @partitionKey;";

        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);
        sqlCommand.Parameters.AddWithValue("@id", $"EVENT^{id}^00000001");
        sqlCommand.Parameters.AddWithValue("@partitionKey", partitionKey);

        return await sqlCommand.ExecuteReaderAsync();
    }
}
