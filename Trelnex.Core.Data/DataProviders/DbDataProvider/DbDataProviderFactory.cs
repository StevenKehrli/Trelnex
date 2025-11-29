using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base factory for creating database-backed data providers using LinqToDB.
/// </summary>
public abstract class DbDataProviderFactory
{
    #region Static Fields

    // JSON serializer configuration for complex property mapping
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    #endregion

    #region Private Fields

    // LinqToDB configuration for database connections
    private readonly DataOptions _dataOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new database data provider factory with connection options and table names.
    /// </summary>
    /// <param name="dataOptions">LinqToDB connection and configuration options.</param>
    protected DbDataProviderFactory(
        DataOptions dataOptions)
    {
        // Configure data options with connection opening callback
        _dataOptions = CloneDataOptions(dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public IDataProvider<TItem> Create<TItem>(
        string typeName,
        string itemTableName,
        string eventTableName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        int? eventTimeToLive = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
        where TItem : BaseItem, new()
    {
        // Create mapping schema for entity-to-table mapping
        var mappingSchema = new MappingSchema();

        // Configure metadata reader to use JSON property names as column names
        mappingSchema.AddMetadataReader(new JsonPropertyNameAttributeReader());

        var fmBuilder = new FluentMappingBuilder(mappingSchema);

        // Configure item table mapping
        var builder = fmBuilder.Entity<TItem>()
            .HasTableName(itemTableName);

        builder
            .Property(item => item.Id).IsPrimaryKey()
            .Property(item => item.PartitionKey).IsPrimaryKey();

        MapItemProperties(builder, blockCipherService);

        // Configure events table mapping
        fmBuilder.Entity<ItemEventWithExpiration>()
            .HasTableName(eventTableName)
            .Property(itemEvent => itemEvent.Id).IsPrimaryKey()
            .Property(itemEvent => itemEvent.PartitionKey).IsPrimaryKey()
            .Property(itemEvent => itemEvent.Changes).HasConversion(
                changes => JsonSerializer.Serialize(changes, _jsonSerializerOptions),
                s => JsonSerializer.Deserialize<PropertyChange[]>(s, _jsonSerializerOptions));

        fmBuilder.Build();

        // Create data options with configured mapping schema
        var dataProviderDataOptions = CloneDataOptions(_dataOptions)
            .UseBeforeConnectionOpened(BeforeConnectionOpened)
            .UseMappingSchema(mappingSchema);

        // Create concrete data provider implementation
        return CreateDataProvider(
            typeName: typeName,
            dataOptions: dataProviderDataOptions,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            eventTimeToLive: eventTimeToLive,
            blockCipherService: blockCipherService,
            logger: logger);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Configures database connection before opening.
    /// </summary>
    /// <param name="dbConnection">Database connection to configure.</param>
    protected abstract void BeforeConnectionOpened(
        DbConnection dbConnection);

    /// <summary>
    /// Creates a concrete data provider implementation for the specified item type.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="typeName">Type name identifier for the entity.</param>
    /// <param name="dataOptions">Configured LinqToDB connection options.</param>
    /// <param name="itemValidator">Optional validator for domain-specific rules.</param>
    /// <param name="commandOperations">Allowed CRUD operations for this provider.</param>
    /// <param name="eventTimeToLive">Optional time-to-live for events in seconds.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive properties.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>Database-specific data provider implementation.</returns>
    protected abstract IDataProvider<TItem> CreateDataProvider<TItem>(
        string typeName,
        DataOptions dataOptions,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        int? eventTimeToLive = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
        where TItem : BaseItem, new();

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a deep copy of DataOptions for isolated configuration.
    /// </summary>
    /// <param name="dataOptions">Source DataOptions to clone.</param>
    /// <returns>New DataOptions instance with identical configuration.</returns>
    private static DataOptions CloneDataOptions(
        DataOptions dataOptions)
    {
        return ((dataOptions as ICloneable).Clone() as DataOptions)!;
    }

    /// <summary>
    /// Determines if a property is a complex type requiring JSON serialization.
    /// </summary>
    /// <param name="propertyInfo">Property to analyze.</param>
    /// <returns>True if the property needs JSON conversion.</returns>
    private static bool IsComplexProperty(
        PropertyInfo propertyInfo)
    {
        // Get underlying type for nullable properties
        var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        // Check if type requires complex JSON handling
        var jsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo(propertyType, _jsonSerializerOptions);

        return jsonTypeInfo?.Kind != JsonTypeInfoKind.None;
    }

    /// <summary>
    /// Determines if a property should be encrypted based on EncryptAttribute.
    /// </summary>
    /// <param name="propertyInfo">Property to check for encryption requirements.</param>
    /// <returns>True if the property should be encrypted.</returns>
    private static bool IsEncryptProperty(
        PropertyInfo propertyInfo)
    {
        // Check for EncryptAttribute presence
        return propertyInfo.GetCustomAttribute<EncryptAttribute>() is not null;
    }

    /// <summary>
    /// Configures LinqToDB entity mapping for all properties of the item type.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="builder">LinqToDB entity mapping builder.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive properties.</param>
    private static void MapItemProperties<TItem>(
        EntityMappingBuilder<TItem> builder,
        IBlockCipherService? blockCipherService)
        where TItem : BaseItem, new()
    {
        // Get all public properties of the item type
        var itemProperties = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Configure mapping for each property
        foreach (var itemProperty in itemProperties)
        {
            // Use reflection to invoke generic MapItemProperty method
            var mapItemPropertyMethod = typeof(DbDataProviderFactory)
                .GetMethod(
                    name: nameof(MapItemProperty),
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(
                    typeof(TItem),
                    itemProperty.PropertyType);

            // Configure mapping for this property
            mapItemPropertyMethod.Invoke(
                null,
                [builder, itemProperty, blockCipherService]);
        }
    }

    /// <summary>
    /// Configures LinqToDB mapping for a single property with appropriate conversion.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <typeparam name="TProperty">Property type being mapped.</typeparam>
    /// <param name="builder">LinqToDB entity mapping builder.</param>
    /// <param name="propertyInfo">Property reflection information.</param>
    /// <param name="blockCipherService">Optional encryption service for encrypted properties.</param>
    private static void MapItemProperty<TItem, TProperty>(
        EntityMappingBuilder<TItem> builder,
        PropertyInfo propertyInfo,
        IBlockCipherService? blockCipherService)
        where TItem : BaseItem, new()
    {
        // Skip properties without JSON mapping attributes
        if (propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>() is null) return;

        // Skip properties marked to be ignored
        if (propertyInfo.GetCustomAttribute<JsonIgnoreAttribute>() is not null) return;

        // Create expression to access the property
        var parameter = Expression.Parameter(typeof(TItem));
        var property = Expression.Property(parameter, propertyInfo.Name);
        var lambda = Expression.Lambda<Func<TItem, TProperty>>(property, parameter);

        // Configure encryption conversion if needed
        if (blockCipherService is not null && IsEncryptProperty(propertyInfo))
        {
            builder
                .Property(lambda)
                .HasConversion(
                    toProvider: value => EncryptedJsonService.EncryptToBase64(value, blockCipherService),
                    toModel: encryptedJson => EncryptedJsonService.DecryptFromBase64<TProperty>(encryptedJson, blockCipherService)!);
        }
        // Configure JSON conversion for complex types
        else if (IsComplexProperty(propertyInfo))
        {
            builder
                .Property(lambda)
                .HasConversion(
                    toProvider: value => JsonSerializer.Serialize(value, _jsonSerializerOptions),
                    toModel: json => JsonSerializer.Deserialize<TProperty>(json, _jsonSerializerOptions)!);
        }
    }

    #endregion
}
