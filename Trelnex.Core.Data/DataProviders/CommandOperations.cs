namespace Trelnex.Core.Data;

/// <summary>
/// Defines the CRUD operations permitted for data provider instances.
/// </summary>
/// <remarks>
/// Bitwise flags enum that allows combining multiple operation permissions to control data access capabilities.
/// </remarks>
[Flags]
public enum CommandOperations
{
    /// <summary>
    /// Read-only access (no modification operations permitted).
    /// </summary>
    Read = 0,

    /// <summary>
    /// Allows creation of new items.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Allows modification of existing items.
    /// </summary>
    Update = 2,

    /// <summary>
    /// Allows soft deletion of items.
    /// </summary>
    Delete = 4,

    /// <summary>
    /// Permits all CRUD operations (Create, Update, and Delete).
    /// </summary>
    All = Create | Update | Delete
}
