using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Trelnex.Core.Data;

/// <summary>
/// Custom JSON type info resolver that only serializes properties marked with TrackChangeAttribute.
/// This resolver filters out all properties except those explicitly marked for change tracking.
/// </summary>
internal class TrackChangeResolver : DefaultJsonTypeInfoResolver
{
    /// <summary>
    /// Customizes the JSON serialization metadata to include only properties marked with TrackChangeAttribute.
    /// </summary>
    /// <param name="type">The type being serialized</param>
    /// <param name="options">The JSON serializer options</param>
    /// <returns>Modified JsonTypeInfo that only includes tracked properties</returns>
    public override JsonTypeInfo GetTypeInfo(
        Type type,
        JsonSerializerOptions options)
    {
        // Get the default type info from the base resolver
        var typeInfo = base.GetTypeInfo(type, options);

        // Only process object types - skip primitives, arrays, etc.
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return typeInfo;

        // Create mapping from JSON property names to PropertyInfo for efficient lookup
        var propertyInfoByJsonName = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Name: p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name, PropertyInfo: p))
            .Where(x => x.Name is not null)
            .ToDictionary(x => x.Name!, x => x.PropertyInfo);

        // Collection to hold properties that should be tracked for changes
        var trackChangeProperties = new List<JsonPropertyInfo>();

        // Examine each property in the type to find those marked for tracking
        foreach (var property in typeInfo.Properties)
        {
            // Get the actual PropertyInfo - first check JSON name mapping, then fallback to direct lookup
            if (propertyInfoByJsonName.TryGetValue(property.Name, out var propertyInfo) is false)
            {
                propertyInfo = type.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
            }

            // Check if the property has the TrackChangeAttribute
            if (propertyInfo?.GetCustomAttribute<TrackChangeAttribute>() is not null)
            {
                trackChangeProperties.Add(property);
            }
        }

        // Replace all properties with only the ones marked for tracking
        // This will result in an empty object if no properties are marked
        typeInfo.Properties.Clear();
        trackChangeProperties.ForEach(p => typeInfo.Properties.Add(p));

        return typeInfo;
    }
}
