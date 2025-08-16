using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base factory for creating database-backed data providers using LinqToDB.
/// </summary>
/// <remarks>
/// Provides common infrastructure for database connectivity, schema validation, and entity mapping
/// with support for JSON serialization and optional field encryption.
/// </remarks>
public abstract class DbDataProviderFactory : IDataProviderFactory
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

    /// <summary>
    /// Table names associated with this factory.
    /// </summary>
    private readonly string[] _tableNames;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the database data provider factory.
    /// </summary>
    /// <param name="dataOptions">LinqToDB connection and configuration options.</param>
    /// <param name="tableNames">Array of table names that this factory will manage and validate.</param>
    protected DbDataProviderFactory(
        DataOptions dataOptions,
        string[] tableNames)
    {
        // Clone the data options and configure the connection opening event
        _dataOptions = CloneDataOptions(dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened);

        // Assign the table names
        _tableNames = tableNames;
    }

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets the SQL query string used to retrieve database version information for health checks.
    /// </summary>
    protected abstract string VersionQueryString { get; }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public IDataProvider<TInterface> Create<TInterface, TItem>(
        string tableName,
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        int? eventTimeToLive = null,
        IBlockCipherService? blockCipherService = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Build the mapping schema
        var mappingSchema = new MappingSchema();

        // Add the metadata reader to handle JSON property name attributes
        mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

        var fmBuilder = new FluentMappingBuilder(mappingSchema);

        // Map the item to its table ("<tableName>")
        var builder = fmBuilder.Entity<TItem>()
            .HasTableName(tableName);

        builder
            .Property(item => item.Id).IsPrimaryKey()
            .Property(item => item.PartitionKey).IsPrimaryKey();

        MapItemProperties<TInterface, TItem>(builder, blockCipherService);

        // Map the event to its table ("<tableName>-events")
        var eventsTableName = GetEventsTableName(tableName);
        fmBuilder.Entity<ItemEventWithExpiration>()
            .HasTableName(eventsTableName)
            .Property(itemEvent => itemEvent.Id).IsPrimaryKey()
            .Property(itemEvent => itemEvent.PartitionKey).IsPrimaryKey()
            .Property(itemEvent => itemEvent.Changes).HasConversion(
                changes => JsonSerializer.Serialize(changes, _jsonSerializerOptions),
                s => JsonSerializer.Deserialize<PropertyChange[]>(s, _jsonSerializerOptions));

        fmBuilder.Build();

        // Configure the data options with the mapping schema
        var dataProviderDataOptions = CloneDataOptions(_dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened)
            .UseMappingSchema(mappingSchema);

        // Create the specific data provider implementation
        return CreateDataProvider<TInterface, TItem>(
            dataOptions: dataProviderDataOptions,
            typeName: typeName,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventTimeToLive: eventTimeToLive);
    }

    /// <inheritdoc/>
#pragma warning disable CS1998
    public async Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        // Create a copy of the status data to avoid modification of the original
        var statusData = GetStatusData();
        var data = new Dictionary<string, object>(statusData);

        try
        {
            using var dataConnection = new DataConnection(_dataOptions);

            // Get the multi-line version string from the database
            var version = dataConnection.Query<string>(VersionQueryString);

            // Split the version into each line, cleaning up whitespace
            var versionArray = version
                .FirstOrDefault()?
                .Split(['\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            // Get the database schema for table validation
            var schemaProvider = dataConnection.DataProvider.GetSchemaProvider();
            var databaseSchema = schemaProvider.GetSchema(dataConnection);

            // Check for any tables missing from the database schema
            var missingTableNames = new List<string>();
            foreach (var tableName in _tableNames.OrderBy(tableName => tableName))
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

            var status = new DataProviderFactoryStatus(
                IsHealthy: missingTableNames.Count == 0,
                Data: data);

            return status;
        }
        catch (Exception ex)
        {
            // Record the exception message in status data
            data["error"] = ex.Message;

            return new DataProviderFactoryStatus(
                IsHealthy: false,
                Data: data);
        }
    }
#pragma warning restore CS1998

    #endregion

    #region Protected Methods

    /// <summary>
    /// Configures the database connection before it is opened.
    /// </summary>
    /// <param name="dbConnection">The database connection to configure with provider-specific settings.</param>
    /// <remarks>
    /// Override this method to set connection-specific properties like timeouts, encryption, or other provider options.
    /// </remarks>
    protected abstract void BeforeConnectionOpened(
        DbConnection dbConnection);

    /// <summary>
    /// Creates the concrete data provider implementation for the specified entity type.
    /// </summary>
    /// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
    /// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
    /// <param name="dataOptions">Configured LinqToDB connection options with mapping schema.</param>
    /// <param name="typeName">Type name identifier for the entity.</param>
    /// <param name="itemValidator">Optional validator for domain-specific validation rules.</param>
    /// <param name="commandOperations">Permitted CRUD operations for this provider.</param>
    /// <param name="eventTimeToLive">Optional time-to-live for events in the table.</param>
    /// <returns>A database-specific data provider implementation.</returns>
    protected abstract IDataProvider<TInterface> CreateDataProvider<TInterface, TItem>(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        int? eventTimeToLive = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();

    /// <summary>
    /// Provides database-specific status information for health monitoring.
    /// </summary>
    /// <returns>A dictionary containing provider-specific diagnostic information.</returns>
    /// <remarks>
    /// Override this method to include database-specific metadata like connection string info,
    /// provider version, or other relevant diagnostic data.
    /// </remarks>
    protected abstract IReadOnlyDictionary<string, object> GetStatusData();

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a deep copy of LinqToDB DataOptions for isolated configuration.
    /// </summary>
    /// <param name="dataOptions">The source DataOptions to clone.</param>
    /// <returns>A new DataOptions instance with identical configuration.</returns>
    private static DataOptions CloneDataOptions(
        DataOptions dataOptions)
    {
        return ((dataOptions as ICloneable).Clone() as DataOptions)!;
    }

    /// <summary>
    /// Generates the events table name by appending '-events' suffix to the item table name.
    /// </summary>
    /// <param name="tableName">The table name for items.</param>
    /// <returns>The corresponding events table name for audit trail storage.</returns>
    private static string GetEventsTableName(
        string tableName)
    {
        return $"{tableName}-events";
    }

    /// <summary>
    /// Configures LinqToDB entity mapping for all properties of the item type using reflection.
    /// </summary>
    /// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
    /// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
    /// <param name="builder">The LinqToDB entity mapping builder.</param>
    /// <param name="blockCipherService">Optional block cipher service for sensitive properties.</param>
    private static void MapItemProperties<TInterface, TItem>(
        EntityMappingBuilder<TItem> builder,
        IBlockCipherService? blockCipherService)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Get all public instance properties of the item type
        var itemProperties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Iterate through each property
        foreach (var itemProperty in itemProperties)
        {
            // Get the MapItemProperty method using reflection
            var mapItemPropertyMethod = typeof(DbDataProviderFactory)
                .GetMethod(
                    name: nameof(MapItemProperty),
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(
                    typeof(TInterface),
                    typeof(TItem),
                    itemProperty.PropertyType);

            // Invoke the MapItemProperty method for the current property
            mapItemPropertyMethod.Invoke(
                null,
                [builder, itemProperty, blockCipherService]);
        }
    }

    /// <summary>
    /// Configures LinqToDB mapping for a single property with appropriate conversion handling.
    /// </summary>
    /// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
    /// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
    /// <typeparam name="TProperty">The property type being mapped.</typeparam>
    /// <param name="builder">The LinqToDB entity mapping builder.</param>
    /// <param name="propertyInfo">Reflection information about the property being mapped.</param>
    /// <param name="blockCipherService">Optional block cipher service for properties marked with EncryptAttribute.</param>
    /// <remarks>
    /// Handles three mapping scenarios: encrypted properties, complex types requiring JSON serialization,
    /// and simple types mapped directly to database columns.
    /// </remarks>
    private static void MapItemProperty<TInterface, TItem, TProperty>(
        EntityMappingBuilder<TItem> builder,
        PropertyInfo propertyInfo,
        IBlockCipherService? blockCipherService)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Skip properties without JsonPropertyNameAttribute
        if (propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>() is null) return;

        // Skip properties with JsonIgnoreAttribute
        if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() is not null) return;

        // Create an expression to access the property
        var parameter = Expression.Parameter(typeof(TItem));
        var property = Expression.Property(parameter, propertyInfo.Name);
        var lambda = Expression.Lambda<Func<TItem, TProperty>>(property, parameter);

        // If block cipher service is available and the property should be encrypted
        if (blockCipherService is not null && IsEncryptProperty(propertyInfo))
        {
            // Configure the property to use encryption and decryption converters
            builder
                .Property(lambda)
                .HasConversion(
                    toProvider: value => EncryptedJsonService.EncryptToBase64(value, blockCipherService),
                    toModel: encryptedJson => EncryptedJsonService.DecryptFromBase64<TProperty>(encryptedJson, blockCipherService)!);
        }
        // If the property is a complex type
        else if (IsComplexProperty(propertyInfo))
        {
            // Configure the property to use JSON serialization and deserialization converters
            builder
                .Property(lambda)
                .HasConversion(
                    toProvider: value => JsonSerializer.Serialize(value, _jsonSerializerOptions),
                    toModel: json => JsonSerializer.Deserialize<TProperty>(json, _jsonSerializerOptions)!);
        }
    }

    /// <summary>
    /// Determines whether a property represents a complex type requiring JSON serialization.
    /// </summary>
    /// <param name="propertyInfo">The property to analyze.</param>
    /// <returns>True if the property is a complex type that needs JSON conversion; otherwise, false.</returns>
    /// <remarks>
    /// Uses System.Text.Json type information to determine if a property is a simple type
    /// that can be directly mapped to a database column or requires JSON serialization.
    /// </remarks>
    private static bool IsComplexProperty(
        PropertyInfo propertyInfo)
    {
        // Get the underlying type if the property is nullable; otherwise, get the property type
        var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        // Create JSON type information for the property type
        var jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(propertyType, _jsonSerializerOptions);

        // Return true if the property is a complex type; otherwise, false
        return jsonTypeInfo?.Kind != JsonTypeInfoKind.None;
    }

    /// <summary>
    /// Determines whether a property should be encrypted based on the presence of EncryptAttribute.
    /// </summary>
    /// <param name="propertyInfo">The property to check for encryption requirements.</param>
    /// <returns>True if the property has EncryptAttribute and should be encrypted; otherwise, false.</returns>
    private static bool IsEncryptProperty(
        PropertyInfo propertyInfo)
    {
        // Check if the property has the EncryptAttribute
        return propertyInfo.GetCustomAttribute<EncryptAttribute>() is not null;
    }

    #endregion
}
