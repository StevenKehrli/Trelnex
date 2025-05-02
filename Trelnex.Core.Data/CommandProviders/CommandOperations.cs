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
    /// Read/create-only provider.
    /// </summary>
    None = 0,

    /// <summary>
    /// Update operations allowed.
    /// </summary>
    Update = 1,

    /// <summary>
    /// Delete operations allowed.
    /// </summary>
    Delete = 2,

    /// <summary>
    /// All operations allowed.
    /// </summary>
    All = Update | Delete
}
