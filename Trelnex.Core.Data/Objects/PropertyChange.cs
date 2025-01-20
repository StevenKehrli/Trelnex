using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

internal class PropertyChange
{
    /// <summary>
    /// The property name of the change.
    /// </summary>
    [JsonPropertyName("propertyName")]
    public required string PropertyName { get; set; }

    /// <summary>
    /// The old value for the property.
    /// </summary>
    [JsonPropertyName("oldValue")]
    public dynamic? OldValue { get; set; }

    /// <summary>
    /// The new value for the property.
    /// </summary>
    [JsonPropertyName("newValue")]
    public dynamic? NewValue { get; set; }
}
