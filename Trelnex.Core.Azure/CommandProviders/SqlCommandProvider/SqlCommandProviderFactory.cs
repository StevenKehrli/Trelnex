using System.Data.Common;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Data.SqlClient;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// A builder for creating an instance of the <see cref="SqlCommandProvider"/>.
/// </summary>
internal class SqlCommandProviderFactory : ICommandProviderFactory
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _connectionString;
    private readonly Action<DbConnection> _connectionInterceptor;
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    private SqlCommandProviderFactory(
        string connectionString,
        Action<DbConnection> connectionInterceptor,
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _connectionString = connectionString;
        _connectionInterceptor = connectionInterceptor;
        _getStatus = getStatus;
    }

    /// <summary>
    /// Create an instance of the <see cref="SqlCommandProviderFactory"/>.
    /// </summary>
    /// <param name="sqlClientOptions">The <see cref="SqlClientOptions"/> options.</param>
    /// <returns>The <see cref="SqlCommandProviderFactory"/>.</returns>
    public static async Task<SqlCommandProviderFactory> Create(
        SqlClientOptions sqlClientOptions)
    {
        // build the data options
        var scsBuilder = new SqlConnectionStringBuilder()
        {
            DataSource = sqlClientOptions.DataSource,
            InitialCatalog = sqlClientOptions.InitialCatalog,
            Encrypt = true,
        };

        // build the connection interceptor
        var connectionInterceptor = new Action<DbConnection>(dbConnection =>
        {
            if (dbConnection is not SqlConnection sqlConnection) return;

            // get the access token
            var tokenRequestContext = new TokenRequestContext([ sqlClientOptions.Scope ]);
            var accessToken = sqlClientOptions.TokenCredential.GetToken(tokenRequestContext, default).Token;

            sqlConnection.AccessToken = accessToken;
        });

        // build the health check
        var dataOptions = new DataOptions()
            .UseSqlServer(scsBuilder.ConnectionString)
            .UseBeforeConnectionOpened(connectionInterceptor);

        CommandProviderFactoryStatus getStatus()
        {
            var data = new Dictionary<string, object>
            {
                { "dataSource", sqlClientOptions.DataSource },
                { "initialCatalog", sqlClientOptions.InitialCatalog },
                { "tableNames", sqlClientOptions.TableNames },
            };

            try
            {
                using var dataConnection = new DataConnection(dataOptions);

                // get the multi-line version string
                var version = dataConnection.Query<string>("SELECT @@VERSION");

                // split the version into each line
                char[] delimiterChars = ['\r', '\n', '\t'];

                var versionArray = version
                    .FirstOrDefault()?
                    .Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);

                // get the database schema
                var schemaProvider = dataConnection.DataProvider.GetSchemaProvider();
                var databaseSchema = schemaProvider.GetSchema(dataConnection);

                // get any tables not in the database schema
                var missingTableNames = new List<string>();
                foreach (var tableName in sqlClientOptions.TableNames.OrderBy(tableName => tableName))
                {
                    // table name
                    if (databaseSchema.Tables.Any(tableSchema => tableSchema.TableName == tableName) is false)
                    {
                        missingTableNames.Add(tableName);
                    }

                    // events table name
                    var eventsTableName = GetEventsTableName(tableName);
                    if (databaseSchema.Tables.Any(tableSchema => tableSchema.TableName == eventsTableName) is false)
                    {
                        missingTableNames.Add(eventsTableName);
                    }
                }

                // set the version
                if (versionArray is not null)
                {
                    data["version"] = versionArray;
                }

                if (0 == missingTableNames.Count)
                {
                    data["error"] = $"Missing Tables: {string.Join(", ", missingTableNames)}";
                }

                return new CommandProviderFactoryStatus(
                    IsHealthy: 0 == missingTableNames.Count,
                    Data: data);
            }
            catch (Exception ex)
            {
                data["error"] = ex.Message;

                return new CommandProviderFactoryStatus(
                    IsHealthy: false,
                    Data: data);
            }
        }

        // warm-up the connection
        var status = getStatus();
        if (status.IsHealthy is false)
        {
            throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                status.Data["error"] as string);
        }

        // build the factory
        var factory = new SqlCommandProviderFactory(
            scsBuilder.ConnectionString,
            connectionInterceptor,
            getStatus);

        return await Task.FromResult(factory);
    }

    /// <summary>
    /// Create an instance of the <see cref="SqlCommandProvider"/>.
    /// </summary>
    /// <param name="tableName">The SQL table as the backing data store.</param>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed. By default, update is allowed; delete is not allowed.</param>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
    /// <returns>The <see cref="SqlCommandProvider"/>.</returns>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string tableName,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // build the mapping schema
        var mappingSchema = new MappingSchema();

        // add the metadata reader
        mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

        mappingSchema.SetConverter<DateTime, DateTimeOffset>(dt => new DateTimeOffset(dt));
        mappingSchema.SetConverter<DateTimeOffset, DateTime>(dto => dto.UtcDateTime);

        var fmBuilder = new FluentMappingBuilder(mappingSchema);

        // map the item to its table ("<tableName>")
        fmBuilder.Entity<TItem>()
            .HasTableName(tableName)
            .Property(e => e.Id).IsPrimaryKey()
            .Property(e => e.PartitionKey).IsPrimaryKey();

        // map the event to its table ("<tableName>-events")
        var eventsTableName = GetEventsTableName(tableName);
        fmBuilder.Entity<ItemEvent<TItem>>()
            .HasTableName(eventsTableName)
            .Property(e => e.Id).IsPrimaryKey()
            .Property(e => e.PartitionKey).IsPrimaryKey()
            .Property(e => e.Changes).HasConversion(
                changes => JsonSerializer.Serialize(changes, _jsonSerializerOptions),
                s => JsonSerializer.Deserialize<PropertyChange[]>(s, _jsonSerializerOptions))
            .Property(e => e.Context).HasConversion(
                context => JsonSerializer.Serialize(context, _jsonSerializerOptions),
                s => JsonSerializer.Deserialize<ItemEventContext>(s, _jsonSerializerOptions) ?? new ItemEventContext());

        fmBuilder.Build();

        // build the data options
        var dataOptions = new DataOptions()
            .UseSqlServer(_connectionString)
            .UseBeforeConnectionOpened(_connectionInterceptor)
            .UseMappingSchema(mappingSchema);

        return new SqlCommandProvider<TInterface, TItem>(
            dataOptions,
            typeName,
            validator,
            commandOperations);
    }

    public CommandProviderFactoryStatus GetStatus() => _getStatus();

    private static string GetEventsTableName(string tableName) => $"{tableName}-events";
}
