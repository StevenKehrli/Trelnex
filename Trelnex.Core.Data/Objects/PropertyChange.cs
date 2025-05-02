using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Records a change to a property.
/// </summary>
/// <remarks>
/// Used in <see cref="ItemEvent"/> to record property modifications.
/// Uses dynamic typing for property values.
/// </remarks>
public class PropertyChange
{
    #region Public Properties

    /// <summary>
    /// Name of the property that was changed.
    /// </summary>
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; init; } = null!;

    /// <summary>
    /// Original value of the property.
    /// </summary>
    [JsonPropertyName("oldValue")]
    public dynamic? OldValue { get; init; }

    /// <summary>
    /// Updated value of the property.
    /// </summary>
    [JsonPropertyName("newValue")]
    public dynamic? NewValue { get; init; }

    #endregion
}
