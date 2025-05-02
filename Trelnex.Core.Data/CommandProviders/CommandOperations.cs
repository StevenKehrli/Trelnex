namespace Trelnex.Core.Data;

/// <summary>
/// Specifies the allowed operations for command providers.
/// </summary>
/// <remarks>
/// This enum is designed as a flags enum to allow combining multiple operation permissions.
/// It is typically used to configure which operations (update, delete) are permitted for
/// specific entity types in the command provider system.
/// </remarks>
[Flags]
public enum CommandOperations
{
    /// <summary>
    /// No operations are allowed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Update operations are allowed.
    /// </summary>
    Update = 1,

    /// <summary>
    /// Delete operations are allowed.
    /// </summary>
    Delete = 2,

    /// <summary>
    /// All operations (Update and Delete) are allowed.
    /// </summary>
    All = Update | Delete
}
