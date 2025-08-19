namespace Trelnex.Core.Data;

/// <summary>
/// Defines the operations that can be performed on data items.
/// </summary>
[Flags]
public enum CommandOperations
{
    /// <summary>
    /// No operations allowed.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Allows creating new items.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Allows updating existing items.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Allows deleting items.
    /// </summary>
    Delete = 4,

    /// <summary>
    /// Allows all operations (Create, Update, and Delete).
    /// </summary>
    All = Create | Update | Delete
}
