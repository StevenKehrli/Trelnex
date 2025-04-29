using System.Data.Common;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base class for database command provider factories.
/// </summary>
public abstract class DbCommandProviderFactory : ICommandProviderFactory
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly DataOptions _dataOptions;

    protected DbCommandProviderFactory(
        DataOptions dataOptions)
    {
        _dataOptions = CloneDataOptions(dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened);
    }

    /// <summary>
    /// Create an instance of the <see cref="ICommandProvider{TInterface}"/>.
    /// </summary>
    /// <param name="tableName">The SQL table as the backing data store.</param>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed. By default, update is allowed; delete is not allowed.</param>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
    /// <returns>The <see cref="ICommandProvider{TInterface}"/>.</returns>
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

        // configure the data options
        var commandProviderDataOptions = CloneDataOptions(_dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened)
            .UseMappingSchema(mappingSchema);

        // create the command provider
        return CreateCommandProvider<TInterface, TItem>(
            commandProviderDataOptions,
            typeName,
            validator,
            commandOperations);
    }

    /// <summary>
    /// Gets the command provider factory status.
    /// </summary>
    /// <returns>The command provider factory status.</returns>
    public CommandProviderFactoryStatus GetStatus()
    {
        // get the status data
        var data = new Dictionary<string, object>(StatusData);

        try
        {
            using var dataConnection = new DataConnection(_dataOptions);

            // get the multi-line version string
            var version = dataConnection.Query<string>(VersionQueryString);

            // split the version into each line
            var versionArray = version
                .FirstOrDefault()?
                .Split([ '\r', '\n', '\t' ], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            // get the database schema
            var schemaProvider = dataConnection.DataProvider.GetSchemaProvider();
            var databaseSchema = schemaProvider.GetSchema(dataConnection);

            // get any tables not in the database schema
            var missingTableNames = new List<string>();
            foreach (var tableName in TableNames.OrderBy(tableName => tableName))
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

            if (0 < missingTableNames.Count)
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

    /// <summary>
    /// Get the key-value pairs describing the command provider factory
    /// </summary>
    protected abstract IReadOnlyDictionary<string, object> StatusData { get; }

    /// <summary>
    /// Get the array of table names required for this command provider factory.
    /// </summary>
    protected abstract string[] TableNames { get; }

    /// <summary>
    /// Get the verion query string for the database.
    /// </summary>
    /// <returns>The version query string.</returns>
    protected abstract string VersionQueryString { get; }

    /// <summary>
    /// Configures the connection to the database.
    /// </summary>
    /// <param name="dbConnection">The <see cref="DbConnection"/>.</param>
    protected abstract void BeforeConnectionOpened(
        DbConnection dbConnection);

    /// <summary>
    /// Create an instance of the <see cref="ICommandProvider"/>.
    /// </summary>
    /// <param name="dataOptions">The <see cref="DataOptions"/>.</param>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed. By default, update is allowed; delete is not allowed.</param>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
    /// <returns>The <see cref="SqlCommandProvider"/>.</returns>
    protected abstract ICommandProvider<TInterface> CreateCommandProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();

    /// <summary>
    /// Checks if the command provider factory is healthy.
    /// </summary>
    protected void IsHealthyOrThrow()
    {
        // warm-up the connection
        var status = GetStatus();
        if (status.IsHealthy is false)
        {
            throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                status.Data["error"] as string);
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="DataOptions"/> class from the existing instance.
    /// </summary>
    /// <returns>The existing <see cref="DataOptions"/>.</returns>
    /// <returns>The new <see cref="DataOptions"/>.</returns>
    private static DataOptions CloneDataOptions(
        DataOptions dataOptions)
    {
        return ((dataOptions as ICloneable).Clone() as DataOptions)!;
    }

    /// <summary>
    /// Get the corresponding events table name for the specified table name.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>The events table name.</returns>
    private static string GetEventsTableName(
        string tableName)
    {
        // get the events table name
        return $"{tableName}-events";
    }
}
