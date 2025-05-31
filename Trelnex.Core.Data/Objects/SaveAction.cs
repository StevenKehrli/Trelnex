using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Type of save operation.
/// </summary>
/// <remarks>
/// Tracks entity lifecycle status. Serialized as a string in JSON.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SaveAction
{
    /// <summary>
    /// Save action not specified.
    /// </summary>
    UNKNOWN = 0,

    /// <summary>
    /// New entity created.
    /// </summary>
    CREATED = 1,

    /// <summary>
    /// Existing entity modified.
    /// </summary>
    UPDATED = 2,

    /// <summary>
    /// Entity removed.
    /// </summary>
    DELETED = 3,
}
