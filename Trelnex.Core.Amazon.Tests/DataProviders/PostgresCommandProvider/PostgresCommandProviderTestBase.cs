using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Encryption;
using Trelnex.Core.Data.Tests.DataProviders;
using Trelnex.Core.Encryption;

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
public abstract class PostgresDataProviderTestBase : DataProviderTests
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
    /// The name of the table used for testing.
    /// </summary>
    protected string _tableName = null!;

    /// <summary>
    /// The name of the encrypted table used for testing.
    /// </summary>
    protected string _encryptedTableName = null!;

    /// <summary>
    /// The block cipher service used for encrypting and decrypting test data.
    /// </summary>
    protected IBlockCipherService _blockCipherService = null!;

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

        // Get the table name from the configuration.
        // Example: "test-items"
        _tableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:test-item:TableName")
            .Get<string>()!;

        // Get the encrypted table name from the configuration.
        // Example: "test-items"
        _encryptedTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:encrypted-test-item:TableName")
            .Get<string>()!;

        // Create the block cipher service from configuration using the factory pattern.
        // This deserializes the algorithm type and settings, then creates the appropriate service.
        _blockCipherService = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:encrypted-test-item")
            .CreateBlockCipherService()!;

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
        TableCleanup(_tableName);
        TableCleanup(_encryptedTableName);
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

        // Define the SQL command to delete all rows from the main table and its events table.
        var cmdText = $"DELETE FROM \"{tableName}-events\"; DELETE FROM \"{tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
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
        string partitionKey)
    {
        // Define the SQL command to get the private message and optional message.
        var cmdText = $"SELECT \"privateMessage\", \"optionalMessage\" FROM \"{_encryptedTableName}\" WHERE \"id\" = @id AND \"partitionKey\" = @partitionKey;";

        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);
        sqlCommand.Parameters.AddWithValue("@id", id);
        sqlCommand.Parameters.AddWithValue("@partitionKey", partitionKey);

        return await sqlCommand.ExecuteReaderAsync();
    }
}
