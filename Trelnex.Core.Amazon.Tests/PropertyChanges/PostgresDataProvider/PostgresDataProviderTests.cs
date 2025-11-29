using System.Data.Common;
using System.Text.Json;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Trelnex.Core.Amazon.DataProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.PropertyChanges;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.PropertyChanges;

[Ignore("Requires a Postgres server.")]
[Category("EventPolicy")]
public class PostgresDataProviderTests : EventPolicyTests
{
    private AWSCredentials _awsCredentials = null!;
    private DataOptions _baseDataOptions = null!;
    private string _connectionString = null!;
    private string _dbUser = null!;
    private string _eventTableName = null!;
    private string _host = null!;
    private string _itemTableName = null!;
    private int _port = 5432;
    private RegionEndpoint _region = null!;

    /// <summary>
    /// Sets up the PostgresDataProvider for testing using the direct constructor instantiation approach.
    /// </summary>
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
        var database = configuration
            .GetSection("Amazon.PostgresDataProviders:Database")
            .Get<string>();

        // Get the database user from the configuration.
        // Example: "admin"
        _dbUser = configuration
            .GetSection("Amazon.PostgresDataProviders:DbUser")
            .Get<string>()!;

        // Get the item table name from the configuration.
        // Example: "test-items"
        _itemTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:test-item:ItemTableName")
            .Get<string>()!;

        // Get the event table name from the configuration.
        // Example: "test-items-events"
        _eventTableName = configuration
            .GetSection("Amazon.PostgresDataProviders:Tables:test-item:EventTableName")
            .Get<string>()!;

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
            ApplicationName = serviceConfiguration.FullName,
            Host = _host,
            Port = _port,
            Database = database,
            Username = _dbUser,
            Password = pwd,
            SslMode = SslMode.Require
        };

        _connectionString = csb.ConnectionString;

        // Create base DataOptions with PostgreSQL connection string
        _baseDataOptions = new DataOptions().UsePostgreSQL(_connectionString);
    }

    /// <summary>
    /// Cleans up the SQL tables after each test.
    /// </summary>
    /// <remarks>
    /// This method ensures test isolation by removing all items from the SQL tables
    /// after each test runs. This prevents state from one test affecting subsequent tests.
    /// </remarks>
    [TearDown]
    public void TestCleanup()
    {
        TableCleanup(_eventTableName);
        TableCleanup(_itemTableName);
    }

    private void BeforeConnectionOpened(
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

    protected override Task<IDataProvider<EventPolicyTestItem>> GetDataProviderAsync(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null)
    {
        // Build configured DataOptions with mapping schema
        var dataOptions = DataOptionsBuilder.Build<EventPolicyTestItem>(
            baseDataOptions: _baseDataOptions,
            beforeConnectionOpened: BeforeConnectionOpened,
            itemTableName: _itemTableName,
            eventTableName: _eventTableName,
            blockCipherService: blockCipherService);

        var dataProvider = new PostgresDataProvider<EventPolicyTestItem>(
            typeName: typeName,
            dataOptions: dataOptions,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService);

        return Task.FromResult<IDataProvider<EventPolicyTestItem>>(dataProvider);
    }

    protected override async Task<ItemEvent[]> GetItemEventsAsync(
        string id,
        string partitionKey)
    {
        // Build DataOptions for querying events table
        var mappingSchema = new MappingSchema();
        mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

        var fmBuilder = new FluentMappingBuilder(mappingSchema);
        var jsonSerializerOptions = new JsonSerializerOptions();

        fmBuilder.Entity<ItemEvent>()
            .HasTableName(_eventTableName)
            .Property(itemEvent => itemEvent.Id).IsPrimaryKey()
            .Property(itemEvent => itemEvent.PartitionKey).IsPrimaryKey()
            .Property(itemEvent => itemEvent.Changes).HasConversion(
                changes => JsonSerializer.Serialize(changes, jsonSerializerOptions),
                s => JsonSerializer.Deserialize<PropertyChange[]>(s, jsonSerializerOptions));

        fmBuilder.Build();

        var dataOptions = _baseDataOptions
            .UseMappingSchema(mappingSchema)
            .UseBeforeConnectionOpened(BeforeConnectionOpened);

        // Create database connection
        using var dataConnection = new DataConnection(dataOptions);

        // Query for specific item by primary key
        var items = dataConnection
            .GetTable<ItemEvent>()
            .Where(i => i.RelatedId == id && i.PartitionKey == partitionKey);

        return await items.ToArrayAsync();
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

    private void TableCleanup(
        string tableName)
    {
        // Establish a SQL connection using token authentication.
        using var sqlConnection = GetConnection();

        // Define the SQL command to delete all rows from the table
        var cmdText = $"DELETE FROM \"{tableName}\";";
        var sqlCommand = new NpgsqlCommand(cmdText, sqlConnection);

        // Execute the SQL command.
        sqlCommand.ExecuteNonQuery();
    }
}
