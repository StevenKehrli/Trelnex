using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents a change made to a property, capturing the old and new values.
/// </summary>
public class PropertyChange
{
    #region Public Properties

    /// <summary>
    /// Gets or sets the name of the property that changed.
    /// </summary>
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; init; } = null!;

    /// <summary>
    /// Gets or sets the value of the property before the change.
    /// </summary>
    [JsonPropertyName("oldValue")]
    public dynamic? OldValue { get; init; }

    /// <summary>
    /// Gets or sets the value of the property after the change.
    /// </summary>
    [JsonPropertyName("newValue")]
    public dynamic? NewValue { get; init; }

    #endregion
}
