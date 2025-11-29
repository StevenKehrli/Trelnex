using System.Data.Common;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Azure.DataProviders;
using Trelnex.Core.Data;
using Trelnex.Core.Data.Tests.PropertyChanges;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Azure.Tests.PropertyChanges;

[Ignore("Requires a SQL server.")]
[Category("EventPolicy")]
public class SqlDataProviderTests : EventPolicyTests
{
    /// <summary>
    /// The scope for the Azure token credential.
    /// </summary>
    private readonly string _scope = "https://database.windows.net/.default";

    private DataOptions _baseDataOptions = null!;
    private string _connectionString = null!;
    private string _eventTableName = null!;
    private string _itemTableName = null!;
    private TokenCredential _tokenCredential = null!;

    /// <summary>
    /// Sets up the SqlDataProvider for testing using the direct constructor instantiation approach.
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
        _itemTableName = configuration
            .GetSection("Azure.SqlDataProviders:Tables:test-item:ItemTableName")
            .Get<string>()!;

        // Get the event table name from the configuration.
        // Example: "test-items-events"
        _eventTableName = configuration
            .GetSection("Azure.SqlDataProviders:Tables:test-item:EventTableName")
            .Get<string>()!;

        // Create the token credential.
        _tokenCredential = new DefaultAzureCredential();

        // Create the SQL connection string.
        var scsBuilder = new SqlConnectionStringBuilder()
        {
            ApplicationName = serviceConfiguration.FullName,
            DataSource = dataSource,
            InitialCatalog = initialCatalog,
            Encrypt = true,
        };

        _connectionString = scsBuilder.ConnectionString;

        // Create base DataOptions with SQL Server connection string
        _baseDataOptions = new DataOptions().UseSqlServer(_connectionString);
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
        // Only process SQL Server connections
        if (dbConnection is not SqlConnection sqlConnection) return;

        // Generate Azure authentication token for SQL Server
        var tokenCredential = _tokenCredential;
        var tokenRequestContext = new TokenRequestContext([ _scope ]);
        var accessToken = tokenCredential.GetToken(tokenRequestContext, default).Token;

        // Set access token for Azure AD authentication
        sqlConnection.AccessToken = accessToken;
    }

    protected override Task<IDataProvider<EventPolicyTestItem>> GetDataProviderAsync(
        string typeName,
        CommandOperations commandOperations,
        EventPolicy eventPolicy,
        IBlockCipherService? blockCipherService = null)
    {
        // Create the data provider using DataOptionsBuilder and constructor
        var dataOptions = DataOptionsBuilder.Build<EventPolicyTestItem>(
            baseDataOptions: _baseDataOptions,
            beforeConnectionOpened: BeforeConnectionOpened,
            itemTableName: _itemTableName,
            eventTableName: _eventTableName,
            blockCipherService: blockCipherService);

        var dataProvider = new SqlDataProvider<EventPolicyTestItem>(
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
        // Create mapping schema for entity-to-table mapping
        var mappingSchema = new MappingSchema();

        // Configure metadata reader to use JSON property names as column names
        mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

        var fmBuilder = new FluentMappingBuilder(mappingSchema);

        var jsonSerializerOptions = new JsonSerializerOptions();

        // Configure events table mapping
        fmBuilder.Entity<ItemEvent>()
            .HasTableName(_eventTableName)
            .Property(itemEvent => itemEvent.Id).IsPrimaryKey()
            .Property(itemEvent => itemEvent.PartitionKey).IsPrimaryKey()
            .Property(itemEvent => itemEvent.Changes).HasConversion(
                changes => JsonSerializer.Serialize(changes, jsonSerializerOptions),
                s => JsonSerializer.Deserialize<PropertyChange[]>(s, jsonSerializerOptions));

        fmBuilder.Build();

        // Create data options with mapping schema
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

    private SqlConnection GetConnection()
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

}
