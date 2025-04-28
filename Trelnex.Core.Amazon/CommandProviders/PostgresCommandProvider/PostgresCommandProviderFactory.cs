using System.Data.Common;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.RDS.Util;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Npgsql;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// A builder for creating an instance of the <see cref="PostgresCommandProvider"/>.
/// </summary>
internal class PostgresCommandProviderFactory : ICommandProviderFactory
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _connectionString;
    private readonly Action<DbConnection> _connectionInterceptor;
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    private PostgresCommandProviderFactory(
        string connectionString,
        Action<DbConnection> connectionInterceptor,
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _connectionString = connectionString;
        _connectionInterceptor = connectionInterceptor;
        _getStatus = getStatus;
    }

    /// <summary>
    /// Create an instance of the <see cref="PostgresCommandProviderFactory"/>.
    /// </summary>
    /// <param name="serviceConfiguration">The <see cref="ServiceConfiguration"/> options.</param>
    /// <param name="postgresClientOptions">The <see cref="PostgresClientOptions"/> options.</param>
    /// <returns>The <see cref="PostgresCommandProviderFactory"/>.</returns>
    public static async Task<PostgresCommandProviderFactory> Create(
        ServiceConfiguration serviceConfiguration,
        PostgresClientOptions postgresClientOptions)
    {
        // build the connection string
        var connectionString = BuildConnectionString(serviceConfiguration, postgresClientOptions);

        // build the connection interceptor
        var connectionInterceptor = new Action<DbConnection>(dbConnection =>
        {
            BeforeConnectionOpened(postgresClientOptions, dbConnection);
        });

        // build the health check
        var dataOptions = new DataOptions()
            .UsePostgreSQL(connectionString)
            .UseBeforeConnectionOpened(connectionInterceptor);

        CommandProviderFactoryStatus getStatus()
        {
            var data = new Dictionary<string, object>
            {
                { "region", postgresClientOptions.Region },
                { "host", postgresClientOptions.Host },
                { "port", postgresClientOptions.Port },
                { "database", postgresClientOptions.Database },
                { "dbUser", postgresClientOptions.DbUser },
                { "tableNames", postgresClientOptions.TableNames },
            };

            try
            {
                using var dataConnection = new DataConnection(dataOptions);

                // get the multi-line version string
                var versionArray = GetVersionArray(dataConnection);

                // get the database schema
                var schemaProvider = dataConnection.DataProvider.GetSchemaProvider();
                var databaseSchema = schemaProvider.GetSchema(dataConnection);

                // get any tables not in the database schema
                var missingTableNames = new List<string>();
                foreach (var tableName in postgresClientOptions.TableNames.OrderBy(tableName => tableName))
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
        var factory = new PostgresCommandProviderFactory(
            connectionString,
            connectionInterceptor,
            getStatus);

        return await Task.FromResult(factory);
    }

    /// <summary>
    /// Create an instance of the <see cref="PostgresCommandProvider"/>.
    /// </summary>
    /// <param name="tableName">The Postgres table as the backing data store.</param>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed. By default, update is allowed; delete is not allowed.</param>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
    /// <returns>The <see cref="PostgresCommandProvider"/>.</returns>
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
            .UsePostgreSQL(_connectionString)
            .UseBeforeConnectionOpened(_connectionInterceptor)
            .UseMappingSchema(mappingSchema);

        return new PostgresCommandProvider<TInterface, TItem>(
            dataOptions,
            typeName,
            validator,
            commandOperations);
    }

    public CommandProviderFactoryStatus GetStatus() => _getStatus();

    private static string GetEventsTableName(string tableName) => $"{tableName}-events";

    /// <summary>
    /// Set the password for the connection string.
    /// </summary>
    /// <param name="clientOptions">The <see cref="PostgresClientOptions"/>.</param>
    /// <param name="dbConnection">The <see cref="DbConnection"/>.</param>
    private static void BeforeConnectionOpened(
        PostgresClientOptions clientOptions,
        DbConnection dbConnection)
    {
        if (dbConnection is not NpgsqlConnection connection) return;

        var regionEndpoint = RegionEndpoint.GetBySystemName(clientOptions.Region);

        var pwd = RDSAuthTokenGenerator.GenerateAuthToken(
            credentials: clientOptions.AWSCredentials,
            region: regionEndpoint,
            hostname: clientOptions.Host,
            port: clientOptions.Port,
            dbUser: clientOptions.DbUser);

        var csb = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
        {
            Password = pwd,
            SslMode = SslMode.Require
        };

        connection.ConnectionString = csb.ConnectionString;
    }

    /// <summary>
    /// Build the connection string.
    /// </summary>
    /// <param name="serviceConfiguration">The <see cref="ServiceConfiguration"/>.</param>
    /// <param name="clientOptions">The <see cref="PostgresClientOptions"/>.</param>
    /// <returns>The connection string.</returns>
    private static string BuildConnectionString(
        ServiceConfiguration serviceConfiguration,
        PostgresClientOptions clientOptions)
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            ApplicationName = serviceConfiguration.FullName,
            Host = clientOptions.Host,
            Port = clientOptions.Port,
            Database = clientOptions.Database,
            Username = clientOptions.DbUser,
            SslMode = SslMode.Require
        };

        return csb.ConnectionString;
    }

    /// <summary>
    /// Get the version array from the database.
    /// </summary>
    /// <param name="dataConnection">The <see cref="DataConnection"/>.</param>
    /// <returns>The version array.</returns>
    private static string[]? GetVersionArray(
        DataConnection dataConnection)
    {
        var version = dataConnection.Query<string>("SELECT version()");

        // split the version into each line
        char[] delimiterChars = ['\r', '\n', '\t'];

        return version
            .FirstOrDefault()?
            .Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
    }
}
