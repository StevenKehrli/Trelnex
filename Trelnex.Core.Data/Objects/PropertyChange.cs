using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

public class PropertyChange
{
    /// <summary>
    /// The property name of the change.
    /// </summary>
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; init; } = null!;

    /// <summary>
    /// The old value for the property.
    /// </summary>
    [JsonPropertyName("oldValue")]
    public dynamic? OldValue { get; init; }

    /// <summary>
    /// The new value for the property.
    /// </summary>
    [JsonPropertyName("newValue")]
    public dynamic? NewValue { get; init; }
}
