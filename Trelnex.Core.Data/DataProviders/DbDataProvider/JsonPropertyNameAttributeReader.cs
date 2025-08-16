using System.Reflection;
using System.Text.Json.Serialization;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

namespace Trelnex.Core.Data;

/// <summary>
/// LinqToDB metadata reader that uses JsonPropertyNameAttribute values to map database column names.
/// </summary>
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
        // Look for JsonPropertyNameAttribute on the member
        var jsonPropertyNameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>();

        // Return empty array if attribute not found
        if (jsonPropertyNameAttribute is null) return [];

        // Create LinqToDB column attribute using the JSON property name
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
