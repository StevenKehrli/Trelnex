using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Trelnex.Core.Data;

/// <summary>
/// A custom <see cref="IJsonTypeInfoResolver"/> that filters properties for change tracking.
/// Only properties decorated with both <see cref="JsonPropertyNameAttribute"/> and <see cref="TrackAttribute"/>
/// are included in the final serialization metadata.
/// </summary>
public class TrackPropertyResolver
    : DefaultJsonTypeInfoResolver
{
    /// <summary>
    /// Customizes the JSON serialization metadata by including only properties marked for tracking.
    /// Properties must have both <see cref="JsonPropertyNameAttribute"/> and <see cref="TrackAttribute"/>.
    /// Only object types are processed.
    /// </summary>
    /// <param name="type">The type being serialized.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>
    /// A <see cref="JsonTypeInfo"/> instance containing only the tracked properties.
    /// </returns>
    public override JsonTypeInfo GetTypeInfo(
        Type type,
        JsonSerializerOptions options)
    {
        // Get the default type info from the base resolver.
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        // Only process object types; skip primitives, arrays, and other non-object types.
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object) return jsonTypeInfo;

        // Collect properties that should be tracked for changes.
        var trackProperties = new List<JsonPropertyInfo>();

        // Iterate through each property to check for required attributes.
        foreach (var property in jsonTypeInfo.Properties)
        {
            // Check for the presence of JsonPropertyNameAttribute.
            var jsonPropertyNameAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                .FirstOrDefault();

            // Skip properties without JsonPropertyNameAttribute.
            if (jsonPropertyNameAttribute is null) continue;

            // Check for the presence of TrackAttribute.
            var trackAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(TrackAttribute), true)
                .FirstOrDefault();

            // Skip properties without TrackAttribute.
            if (trackAttribute is null) continue;

            // Add property to the list if both attributes are present.
            trackProperties.Add(property);
        }

        // Replace the property list with only the tracked properties.
        jsonTypeInfo.Properties.Clear();
        trackProperties.ForEach(p => jsonTypeInfo.Properties.Add(p));

        return jsonTypeInfo;
    }
}
