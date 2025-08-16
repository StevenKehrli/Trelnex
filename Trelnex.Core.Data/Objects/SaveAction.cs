using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents the type of save operation performed on an item.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SaveAction
{
    /// <summary>
    /// The save action is unknown or not specified.
    /// </summary>
    UNKNOWN = 0,

    /// <summary>
    /// A new item was created.
    /// </summary>
    CREATED = 1,

    /// <summary>
    /// An existing item was updated.
    /// </summary>
    UPDATED = 2,

    /// <summary>
    /// An existing item was deleted.
    /// </summary>
    DELETED = 3,
}
