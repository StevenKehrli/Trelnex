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

    /// <summary>
    /// Table names associated with this factory.
    /// </summary>
    private readonly string[] _tableNames;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="dataOptions">Database connection options.</param>
    /// <param name="tableNames">Table names associated with this factory.</param>
    protected DbCommandProviderFactory(
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
    /// <param name="encryptionService">Optional encryption service for encrypting sensitive data.</param>
    /// <returns>Configured command provider.</returns>
    /// <exception cref="ArgumentException">When typeName is invalid or reserved.</exception>
    /// <inheritdoc/>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string tableName,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null,
        IEncryptionService? encryptionService = null)
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

        MapItemProperties<TInterface, TItem>(builder, encryptionService);

        // Map the event to its table ("<tableName>-events")
        var eventsTableName = GetEventsTableName(tableName);
        fmBuilder.Entity<ItemEvent<TItem>>()
            .HasTableName(eventsTableName)
            .Property(itemEvent => itemEvent.Id).IsPrimaryKey()
            .Property(itemEvent => itemEvent.PartitionKey).IsPrimaryKey()
            .Property(itemEvent => itemEvent.Changes).HasConversion(
                changes => JsonSerializer.Serialize(changes, _jsonSerializerOptions),
                s => JsonSerializer.Deserialize<PropertyChange[]>(s, _jsonSerializerOptions));

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
    /// Asynchronously gets the current operational status of the factory.
    /// </summary>
    /// <param name="cancellationToken">A token that may be used to cancel the operation.</param>
    /// <returns>Status information including connectivity and container availability.</returns>
#pragma warning disable CS1998
    public async Task<CommandProviderFactoryStatus> GetStatusAsync(
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

            var status = new CommandProviderFactoryStatus(
                IsHealthy: missingTableNames.Count == 0,
                Data: data);

            return status;
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
#pragma warning restore CS1998

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

    protected abstract IReadOnlyDictionary<string, object> GetStatusData();

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

    /// <summary>
    /// Maps the properties of an item to the database table.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    /// <typeparam name="TItem">The item type implementing the interface.</typeparam>
    /// <param name="builder">The entity mapping builder.</param>
    /// <param name="encryptionService">The encryption service.</param>
    private static void MapItemProperties<TInterface, TItem>(
        EntityMappingBuilder<TItem> builder,
        IEncryptionService? encryptionService)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        // Get all public instance properties of the item type
        var itemProperties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Iterate through each property
        foreach (var itemProperty in itemProperties)
        {
            // Get the MapItemProperty method using reflection
            var mapItemPropertyMethod = typeof(DbCommandProviderFactory)
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
                [builder, itemProperty, encryptionService]);
        }
    }

    /// <summary>
    /// Maps a single property of an item to the database table.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    /// <typeparam name="TItem">The item type implementing the interface.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="builder">The entity mapping builder.</param>
    /// <param name="propertyInfo">The property information.</param>
    /// <param name="encryptionService">The encryption service.</param>
    private static void MapItemProperty<TInterface, TItem, TProperty>(
        EntityMappingBuilder<TItem> builder,
        PropertyInfo propertyInfo,
        IEncryptionService? encryptionService)
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

        // If encryption service is available and the property should be encrypted
        if (encryptionService is not null && IsEncryptProperty(propertyInfo))
        {
            // Configure the property to use encryption and decryption converters
            builder
                .Property(lambda)
                .HasConversion(
                    value => EncryptedJsonService.EncryptToBase64(value, encryptionService),
                    encryptedValue => EncryptedJsonService.DecryptFromBase64<TProperty>(encryptedValue, encryptionService)!);
        }
        // If the property is a complex type
        else if (IsComplexProperty(propertyInfo))
        {
            // Configure the property to use JSON serialization and deserialization converters
            builder
                .Property(lambda)
                .HasConversion(
                    value => JsonSerializer.Serialize(value, _jsonSerializerOptions),
                    encryptedValue => JsonSerializer.Deserialize<TProperty>(encryptedValue, _jsonSerializerOptions)!);
        }
    }

    /// <summary>
    /// Determines whether the property is a complex type.
    /// </summary>
    /// <param name="propertyInfo">The property information.</param>
    /// <returns>True if the property is a complex type; otherwise, false.</returns>
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
    /// Determines whether the property should be encrypted.
    /// </summary>
    /// <param name="propertyInfo">The property information.</param>
    /// <returns>True if the property should be encrypted; otherwise, false.</returns>
    private static bool IsEncryptProperty(
        PropertyInfo propertyInfo)
    {
        // Check if the property has the EncryptAttribute
        return propertyInfo.GetCustomAttribute<EncryptAttribute>() is not null;
    }

    #endregion
}
