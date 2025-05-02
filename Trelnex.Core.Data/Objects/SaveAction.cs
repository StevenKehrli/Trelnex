using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents the type of save operation that has been performed on an entity.
/// </summary>
/// <remarks>
/// This enumeration is used to track the lifecycle status of entities during persistence operations.
/// The enum is configured to be serialized as a string in JSON to improve readability of serialized data.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SaveAction
{
    /// <summary>
    /// Indicates that the save action is not specified or is in an indeterminate state.
    /// </summary>
    UNKNOWN = 0,

    /// <summary>
    /// Indicates that a new entity has been created.
    /// </summary>
    CREATED = 1,

    /// <summary>
    /// Indicates that an existing entity has been modified.
    /// </summary>
    UPDATED = 2,

    /// <summary>
    /// Indicates that an entity has been removed.
    /// </summary>
    DELETED = 3,
}
