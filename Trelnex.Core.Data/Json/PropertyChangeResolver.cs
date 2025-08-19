using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Trelnex.Core.Json;

namespace Trelnex.Core.Data;

/// <summary>
/// A JSON property resolver that filters properties for change tracking based on attributes.
/// Includes properties with <see cref="JsonPropertyNameAttribute"/> while respecting
/// <see cref="TrackAttribute"/> and <see cref="DoNotTrackAttribute"/> for inclusion/exclusion control.
/// </summary>
/// <param name="allChanges">
/// When true, includes all properties with <see cref="JsonPropertyNameAttribute"/> except those marked with <see cref="DoNotTrackAttribute"/>.
/// When false, only includes properties that have both <see cref="JsonPropertyNameAttribute"/> and <see cref="TrackAttribute"/>.
/// </param>
public class PropertyChangeResolver(
    bool allChanges)
    : JsonPropertyResolver
{
    public override IList<JsonPropertyInfo> ConfigureProperties(
        IList<JsonPropertyInfo> properties)
    {
        // Filter properties based on tracking attributes
        var trackProperties = new List<JsonPropertyInfo>();

        // Evaluate each property for change tracking eligibility
        foreach (var property in properties)
        {
            // Require JsonPropertyNameAttribute for all tracked properties
            var jsonPropertyNameAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                .FirstOrDefault();

            // Skip properties without JsonPropertyNameAttribute
            if (jsonPropertyNameAttribute is null) continue;

            // Exclude properties explicitly marked as DoNotTrack
            var doNotTrackAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(DoNotTrackAttribute), true)
                .FirstOrDefault();

            // Skip properties marked with DoNotTrackAttribute
            if (doNotTrackAttribute is not null) continue;

            // Check for explicit TrackAttribute when selective tracking is enabled
            var trackAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(TrackAttribute), true)
                .FirstOrDefault();

            // Apply tracking logic based on allChanges setting
            if (allChanges is false && trackAttribute is null) continue;

            // Include property that meets all tracking criteria
            trackProperties.Add(property);
        }

        return trackProperties;
    }
}
