namespace Trelnex.Core.Data;

/// <summary>
/// Allowed modification operations.
/// </summary>
/// <remarks>
/// Flags enum for combining operation permissions.
/// </remarks>
[Flags]
public enum CommandOperations
{
    /// <summary>
    /// Read operations allowed.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Create operations allowed.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Update operations allowed.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Delete operations allowed.
    /// </summary>
    Delete = 4,

    /// <summary>
    /// All operations allowed.
    /// </summary>
    All = Create | Update | Delete
}
