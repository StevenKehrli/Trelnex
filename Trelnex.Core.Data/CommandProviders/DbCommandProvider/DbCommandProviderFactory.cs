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
/// Provides core functionality for creating and managing database command providers.
/// </summary>
/// <remarks>
/// This class handles database connection management, entity mapping,
/// and provides health check capabilities for the database.
/// </remarks>
public abstract class DbCommandProviderFactory : ICommandProviderFactory
{
    #region Static Fields

    /// <summary>
    /// JSON serializer options used for serializing and deserializing event data.
    /// </summary>
    /// <remarks>
    /// These options ensure consistent handling of null values and proper JSON encoding.
    /// </remarks>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    #endregion

    #region Private Fields

    /// <summary>
    /// Data connection options for this command provider factory.
    /// </summary>
    private readonly DataOptions _dataOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="dataOptions">The database connection options to use.</param>
    /// <remarks>
    /// Creates a clone of the provided data options and configures connection behavior.
    /// </remarks>
    protected DbCommandProviderFactory(
        DataOptions dataOptions)
    {
        _dataOptions = CloneDataOptions(dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened);
    }

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets key-value pairs describing the command provider factory.
    /// </summary>
    /// <value>
    /// A read-only dictionary containing metadata about this command provider factory.
    /// </value>
    protected abstract IReadOnlyDictionary<string, object> StatusData { get; }

    /// <summary>
    /// Gets an array of table names required for this command provider factory.
    /// </summary>
    /// <value>
    /// An array of table names that must exist in the database for proper operation.
    /// </value>
    protected abstract string[] TableNames { get; }

    /// <summary>
    /// Gets the SQL query string used to retrieve the database version.
    /// </summary>
    /// <value>
    /// A SQL query string that returns version information from the database.
    /// </value>
    protected abstract string VersionQueryString { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates an instance of the <see cref="ICommandProvider{TInterface}"/> for a specific entity type.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type that defines the entity contract.</typeparam>
    /// <typeparam name="TItem">The concrete item type that implements the interface and extends <see cref="BaseItem"/>.</typeparam>
    /// <param name="tableName">The SQL table name to use as the backing data store.</param>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">Optional fluent validator for validating items before persistence.</param>
    /// <param name="commandOperations">
    /// Optional flags specifying allowed operations.
    /// By default, update is allowed; delete is not allowed.
    /// </param>
    /// <returns>A configured command provider for the specified entity type.</returns>
    /// <remarks>
    /// This method configures entity mappings, sets up converters for date handling,
    /// and creates appropriate table mappings for both the main entity and its event history.
    /// </remarks>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string tableName,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Build the mapping schema
        var mappingSchema = new MappingSchema();

        // Add the metadata reader to handle JSON property name attributes
        mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

        // Set up date/time converters for consistent UTC handling
        mappingSchema.SetConverter<DateTime, DateTimeOffset>(dt => new DateTimeOffset(dt));
        mappingSchema.SetConverter<DateTimeOffset, DateTime>(dto => dto.UtcDateTime);

        var fmBuilder = new FluentMappingBuilder(mappingSchema);

        // Map the item to its table ("<tableName>")
        fmBuilder.Entity<TItem>()
            .HasTableName(tableName)
            .Property(e => e.Id).IsPrimaryKey()
            .Property(e => e.PartitionKey).IsPrimaryKey();

        // Map the event to its table ("<tableName>-events")
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

        // Configure the data options with the mapping schema
        var commandProviderDataOptions = CloneDataOptions(_dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened)
            .UseMappingSchema(mappingSchema);

        // Create the specific command provider implementation
        return CreateCommandProvider<TInterface, TItem>(
            commandProviderDataOptions,
            typeName,
            validator,
            commandOperations);
    }

    /// <summary>
    /// Gets the current status of the command provider factory.
    /// </summary>
    /// <returns>A <see cref="CommandProviderFactoryStatus"/> object indicating health and metadata.</returns>
    /// <remarks>
    /// This method performs connectivity tests, version checks, and verifies required tables
    /// exist in the database schema. The returned status contains both health state and
    /// detailed metadata about the database.
    /// </remarks>
    public CommandProviderFactoryStatus GetStatus()
    {
        // Create a copy of the status data to avoid modification of the original
        var data = new Dictionary<string, object>(StatusData);

        try
        {
            using var dataConnection = new DataConnection(_dataOptions);

            // Get the multi-line version string from the database
            var version = dataConnection.Query<string>(VersionQueryString);

            // Split the version into each line, cleaning up whitespace
            var versionArray = version
                .FirstOrDefault()?
                .Split([ '\r', '\n', '\t' ], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            // Get the database schema for table validation
            var schemaProvider = dataConnection.DataProvider.GetSchemaProvider();
            var databaseSchema = schemaProvider.GetSchema(dataConnection);

            // Check for any tables missing from the database schema
            var missingTableNames = new List<string>();
            foreach (var tableName in TableNames.OrderBy(tableName => tableName))
            {
                // Check main table existence
                if (databaseSchema.Tables.Any(tableSchema => tableSchema.TableName == tableName) is false)
                {
                    missingTableNames.Add(tableName);
                }

                // Check events table existence
                var eventsTableName = GetEventsTableName(tableName);
                if (databaseSchema.Tables.Any(tableSchema => tableSchema.TableName == eventsTableName) is false)
                {
                    missingTableNames.Add(eventsTableName);
                }
            }

            // Include version information if available
            if (versionArray is not null)
            {
                data["version"] = versionArray;
            }

            // Include error information if any tables are missing
            if (missingTableNames.Count > 0)
            {
                data["error"] = $"Missing Tables: {string.Join(", ", missingTableNames)}";
            }

            return new CommandProviderFactoryStatus(
                IsHealthy: missingTableNames.Count == 0,
                Data: data);
        }
        catch (Exception ex)
        {
            // Record the exception message in status data
            data["error"] = ex.Message;

            return new CommandProviderFactoryStatus(
                IsHealthy: false,
                Data: data);
        }
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Configures the database connection before it is opened.
    /// </summary>
    /// <param name="dbConnection">The database connection to configure.</param>
    /// <remarks>
    /// Implementations should use this method to set connection-specific properties
    /// like timeouts, connection string parameters, or other provider-specific settings.
    /// </remarks>
    protected abstract void BeforeConnectionOpened(
        DbConnection dbConnection);

    /// <summary>
    /// Creates an instance of the appropriate <see cref="ICommandProvider{TInterface}"/> implementation.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the interface.</typeparam>
    /// <param name="dataOptions">The data connection options to use.</param>
    /// <param name="typeName">The type name of the item.</param>
    /// <param name="validator">Optional validator for the item type.</param>
    /// <param name="commandOperations">Optional flags specifying allowed operations.</param>
    /// <returns>An implementation of <see cref="ICommandProvider{TInterface}"/>.</returns>
    /// <remarks>
    /// Concrete factories must implement this method to return their specific command provider implementation.
    /// </remarks>
    protected abstract ICommandProvider<TInterface> CreateCommandProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();

    /// <summary>
    /// Checks if the command provider factory is healthy and throws an exception if not.
    /// </summary>
    /// <exception cref="CommandException">
    /// Thrown when the command provider factory is not healthy, with the error
    /// details and a ServiceUnavailable status code.
    /// </exception>
    /// <remarks>
    /// This method is useful for fail-fast behavior when creating command providers.
    /// </remarks>
    protected void IsHealthyOrThrow()
    {
        // Perform a health check
        var status = GetStatus();
        if (status.IsHealthy is false)
        {
            throw new CommandException(
                HttpStatusCode.ServiceUnavailable,
                status.Data["error"] as string);
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="DataOptions"/> class from the existing instance.
    /// </summary>
    /// <param name="dataOptions">The source data options object to clone.</param>
    /// <returns>A new instance of <see cref="DataOptions"/> with the same configuration.</returns>
    /// <remarks>
    /// Uses the ICloneable interface to create a deep copy of the DataOptions object.
    /// </remarks>
    private static DataOptions CloneDataOptions(
        DataOptions dataOptions)
    {
        return ((dataOptions as ICloneable).Clone() as DataOptions)!;
    }

    /// <summary>
    /// Gets the corresponding events table name for the specified table name.
    /// </summary>
    /// <param name="tableName">The base table name.</param>
    /// <returns>The derived events table name.</returns>
    /// <remarks>
    /// Events tables follow the naming convention of "<tableName>-events".
    /// </remarks>
    private static string GetEventsTableName(
        string tableName)
    {
        return $"{tableName}-events";
    }

    #endregion
}
