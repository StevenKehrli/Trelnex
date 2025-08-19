using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventPolicy
{
    /// <summary>
    /// Event tracking is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// No changes are tracked.
    /// </summary>
    NoChanges,

    /// <summary>
    /// Only properties explicitly marked with TrackAttribute are tracked.
    /// </summary>
    OnlyTrackAttributeChanges,

    /// <summary>
    /// All properties with JsonPropertyNameAttribute are tracked, except those marked with DoNotTrackAttribute.
    /// </summary>
    AllChanges
}
