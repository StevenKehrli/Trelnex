using System.Reflection;
using System.Text.Json.Serialization;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

namespace Trelnex.Core.Data;

/// <summary>
/// Maps database columns from <see cref="JsonPropertyNameAttribute"/> values.
/// </summary>
/// <remarks>
/// Allows entities with JSON serialization attributes to be mapped to database columns.
/// </remarks>
public class JsonPropertyNameAttributeReader : IMetadataReader
{
    #region IMetadataReader Implementation

    /// <inheritdoc/>
    public MappingAttribute[] GetAttributes(Type type) => [];

    /// <inheritdoc/>
    public MappingAttribute[] GetAttributes(
        Type type,
        MemberInfo memberInfo)
    {
        // Check if the member has the JsonPropertyNameAttribute
        var jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();

        // If no JsonPropertyNameAttribute is found, return an empty array
        if (jsonPropertyNameAttribute is null) return [];

        // Create a ColumnAttribute with the name from JsonPropertyNameAttribute
        var columnAttribute = new ColumnAttribute()
        {
            Name = jsonPropertyNameAttribute.Name,
        };

        return [ columnAttribute ];
    }

    /// <inheritdoc/>
    public MemberInfo[] GetDynamicColumns(Type type) => [];

    /// <inheritdoc/>
    public string GetObjectID() => $".{nameof(JsonPropertyNameAttributeReader)}.";

    #endregion
}
