using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using LinqToDB;
using LinqToDB.Mapping;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// Helper for building configured DataOptions with mapping schema for database providers.
/// </summary>
public static class DataOptionsBuilder
{
    #region Static Fields

    // JSON serializer configuration for complex property mapping
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Builds a configured DataOptions with mapping schema for the specified item type.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="baseDataOptions">Base DataOptions with database connection configuration.</param>
    /// <param name="beforeConnectionOpened">Callback to configure connection before opening.</param>
    /// <param name="itemTableName">Physical table name for items.</param>
    /// <param name="eventTableName">Physical table name for events, or null if events are not tracked.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive properties.</param>
    /// <returns>Configured DataOptions with mapping schema.</returns>
    public static DataOptions Build<TItem>(
        DataOptions baseDataOptions,
        Action<DbConnection> beforeConnectionOpened,
        string itemTableName,
        string? eventTableName = null,
        IBlockCipherService? blockCipherService = null)
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

        // Configure events table mapping if event table is specified
        if (eventTableName is not null)
        {
            fmBuilder.Entity<ItemEventWithExpiration>()
                .HasTableName(eventTableName)
                .Property(itemEvent => itemEvent.Id).IsPrimaryKey()
                .Property(itemEvent => itemEvent.PartitionKey).IsPrimaryKey()
                .Property(itemEvent => itemEvent.Changes).HasConversion(
                    changes => JsonSerializer.Serialize(changes, _jsonSerializerOptions),
                    s => JsonSerializer.Deserialize<PropertyChange[]>(s, _jsonSerializerOptions));
        }

        fmBuilder.Build();

        // Create data options with configured mapping schema
        var dataOptions = CloneDataOptions(baseDataOptions)
            .UseBeforeConnectionOpened(beforeConnectionOpened)
            .UseMappingSchema(mappingSchema);

        return dataOptions;
    }

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
            var mapItemPropertyMethod = typeof(DataOptionsBuilder)
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
