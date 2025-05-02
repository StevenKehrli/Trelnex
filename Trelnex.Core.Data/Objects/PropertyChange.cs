using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents a change to a single property of an item tracked by the system.
/// </summary>
/// <remarks>
/// <para>
/// PropertyChange objects are used in the <see cref="ItemEvent{TItem}"/> class to record
/// the specific modifications made to an item's properties during update operations.
/// </para>
/// <para>
/// The class uses <see langword="dynamic"/> for the old and new values to support properties
/// of any type. This provides flexibility but requires careful handling when deserializing
/// or comparing values.
/// </para>
/// </remarks>
/// <seealso cref="ItemEvent{TItem}"/>
public class PropertyChange
{
    #region Properties

    /// <summary>
    /// Gets or sets the name of the property that was changed.
    /// </summary>
    /// <value>
    /// A string containing the name of the property.
    /// </value>
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; init; } = null!;

    /// <summary>
    /// Gets or sets the original value of the property before the change.
    /// </summary>
    /// <value>
    /// The previous value of the property, or <see langword="null"/> if the property was newly added.
    /// </value>
    /// <remarks>
    /// This property uses <see langword="dynamic"/> to support values of any type.
    /// </remarks>
    [JsonPropertyName("oldValue")]
    public dynamic? OldValue { get; init; }

    /// <summary>
    /// Gets or sets the updated value of the property after the change.
    /// </summary>
    /// <value>
    /// The new value of the property, or <see langword="null"/> if the property was removed.
    /// </value>
    /// <remarks>
    /// This property uses <see langword="dynamic"/> to support values of any type.
    /// </remarks>
    [JsonPropertyName("newValue")]
    public dynamic? NewValue { get; init; }

    #endregion
}
