using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Trelnex.Core.Json;

namespace Trelnex.Core.Data;

/// <summary>
/// JSON type info resolver that includes only properties marked with both JsonPropertyNameAttribute and TrackAttribute.
/// </summary>
public class TrackPropertyResolver
    : JsonPropertyResolver
{
    public override IList<JsonPropertyInfo> ConfigureProperties(
        IList<JsonPropertyInfo> properties)
    {
        // List to store properties that meet the tracking criteria
        var trackProperties = new List<JsonPropertyInfo>();

        // Check each property for required attributes
        foreach (var property in properties)
        {
            // Look for JsonPropertyNameAttribute on the property
            var jsonPropertyNameAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                .FirstOrDefault();

            // Skip if JsonPropertyNameAttribute is not present
            if (jsonPropertyNameAttribute is null) continue;

            // Look for DoNotTrackAttribute on the property
            var doNotTrackAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(DoNotTrackAttribute), true)
                .FirstOrDefault();

            // Skip if DoNotTrackAttribute is present
            if (doNotTrackAttribute is not null) continue;

            // Look for TrackAttribute on the property
            var trackAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(TrackAttribute), true)
                .FirstOrDefault();

            // Skip if TrackAttribute is not present
            if (trackAttribute is null) continue;

            // Include property since it has both required attributes
            trackProperties.Add(property);
        }

        return trackProperties;
    }
}
