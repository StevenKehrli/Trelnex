using System.Reflection;
using System.Text.Json.Serialization;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides mapping information for database columns based on <see cref="JsonPropertyNameAttribute"/>.
/// This class converts JSON property names to database column names.
/// </summary>
/// <remarks>
/// This implementation allows entities decorated with <see cref="JsonPropertyNameAttribute"/>
/// to be properly mapped to database columns without requiring separate database mapping attributes.
/// </remarks>
public class JsonPropertyNameAttributeReader : IMetadataReader
{
    #region IMetadataReader Implementation

    /// <summary>
    /// Gets mapping attributes for the specified type.
    /// </summary>
    /// <param name="type">The type to inspect for mapping attributes.</param>
    /// <returns>An empty array as this reader does not process type-level attributes.</returns>
    public MappingAttribute[] GetAttributes(Type type) => [];

    /// <summary>
    /// Gets mapping attributes for a specified member of a type by examining <see cref="JsonPropertyNameAttribute"/>.
    /// </summary>
    /// <param name="type">The containing type.</param>
    /// <param name="memberInfo">The member to inspect for attributes.</param>
    /// <returns>
    /// An array containing a <see cref="ColumnAttribute"/> if a <see cref="JsonPropertyNameAttribute"/> is found;
    /// otherwise, an empty array.
    /// </returns>
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

    /// <summary>
    /// Gets dynamic columns for the specified type.
    /// </summary>
    /// <param name="type">The type to inspect for dynamic columns.</param>
    /// <returns>An empty array as this reader does not support dynamic columns.</returns>
    public MemberInfo[] GetDynamicColumns(Type type) => [];

    /// <summary>
    /// Gets a unique identifier for this metadata reader instance.
    /// </summary>
    /// <returns>A string that uniquely identifies this metadata reader.</returns>
    public string GetObjectID() => $".{nameof(JsonPropertyNameAttributeReader)}.";

    #endregion
}
