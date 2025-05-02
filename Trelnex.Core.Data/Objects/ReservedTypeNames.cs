namespace Trelnex.Core.Data;

/// <summary>
/// Maintains a collection of type names that are reserved for system use and cannot be used by custom types.
/// </summary>
/// <remarks>
/// This class prevents naming conflicts between user-defined types and system-defined types.
/// For example, "event" is reserved for use by the <see cref="ItemEvent{TItem}"/> class, which
/// uses this value as its <see cref="BaseItem.TypeName"/> to represent event records in the system.
/// </remarks>
/// <seealso cref="ItemEvent{TItem}"/>
internal static class ReservedTypeNames
{
    #region Static Fields

    /// <summary>
    /// The reserved name "event" used by <see cref="ItemEvent{TItem}"/> instances to identify event records.
    /// </summary>
    /// <remarks>
    /// This value is assigned to the <see cref="BaseItem.TypeName"/> property of all
    /// <see cref="ItemEvent{TItem}"/> instances during creation to mark them as system events.
    /// </remarks>
    internal static readonly string Event = "event";

    /// <summary>
    /// Collection of all type names that are reserved for system use.
    /// </summary>
    private static readonly string[] _reservedTypeNames = [ Event ];

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines whether the specified type name is reserved for system use.
    /// </summary>
    /// <param name="typeName">The type name to check against the reserved list.</param>
    /// <returns>
    /// <see langword="true"/> if the type name is reserved; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsReserved(
        string typeName)
    {
        return _reservedTypeNames.Any(rtn => string.Equals(rtn, typeName));
    }

    #endregion
}
