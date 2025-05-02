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
/// Base factory for database command providers.
/// </summary>
/// <remarks>
/// Provides infrastructure for database-backed command providers.
/// </remarks>
public abstract class DbCommandProviderFactory : ICommandProviderFactory
{
    #region Static Fields

    /// <summary>
    /// JSON serializer options.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    #endregion

    #region Private Fields

    /// <summary>
    /// Connection options.
    /// </summary>
    private readonly DataOptions _dataOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="dataOptions">Database connection options.</param>
    protected DbCommandProviderFactory(
        DataOptions dataOptions)
    {
        _dataOptions = CloneDataOptions(dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened);
    }

    #endregion

    #region Protected Properties

    /// <summary>
    /// Metadata.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, object> StatusData { get; }

    /// <summary>
    /// Table names.
    /// </summary>
    protected abstract string[] TableNames { get; }

    /// <summary>
    /// SQL query to retrieve database version.
    /// </summary>
    protected abstract string VersionQueryString { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a database-backed command provider.
    /// </summary>
    /// <typeparam name="TInterface">Interface type.</typeparam>
    /// <typeparam name="TItem">Concrete implementation type.</typeparam>
    /// <param name="tableName">Database table name.</param>
    /// <param name="typeName">Type name identifier.</param>
    /// <param name="validator">Optional validator.</param>
    /// <param name="commandOperations">Optional allowed operation flags.</param>
    /// <returns>Configured command provider.</returns>
    /// <exception cref="ArgumentException">When typeName is invalid or reserved.</exception>
    /// <inheritdoc/>
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
    /// Gets current operational status.
    /// </summary>
    /// <returns>Status object with health information.</returns>
    /// <inheritdoc/>
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
    /// Configures database connection.
    /// </summary>
    /// <param name="dbConnection">Database connection to configure.</param>
    protected abstract void BeforeConnectionOpened(
        DbConnection dbConnection);

    /// <summary>
    /// Creates command provider implementation.
    /// </summary>
    /// <typeparam name="TInterface">Interface type.</typeparam>
    /// <typeparam name="TItem">Item type implementing the interface.</typeparam>
    /// <param name="dataOptions">Connection options to use.</param>
    /// <param name="typeName">Type name of the item.</param>
    /// <param name="validator">Optional validator.</param>
    /// <param name="commandOperations">Optional allowed operation flags.</param>
    /// <returns>Command provider implementation.</returns>
    protected abstract ICommandProvider<TInterface> CreateCommandProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();

    /// <summary>
    /// Verifies factory health or throws exception.
    /// </summary>
    /// <exception cref="CommandException">
    /// Thrown when factory is unhealthy.
    /// </exception>
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
    /// Clones a DataOptions object.
    /// </summary>
    /// <param name="dataOptions">Source options to clone.</param>
    /// <returns>New DataOptions with same configuration.</returns>
    private static DataOptions CloneDataOptions(
        DataOptions dataOptions)
    {
        return ((dataOptions as ICloneable).Clone() as DataOptions)!;
    }

    /// <summary>
    /// Gets events table name.
    /// </summary>
    /// <param name="tableName">Base table name.</param>
    /// <returns>Events table name (tableName-events).</returns>
    private static string GetEventsTableName(
        string tableName)
    {
        return $"{tableName}-events";
    }

    #endregion
}
