namespace Trelnex.Core.Data;

/// <summary>
/// Contains type names that are reserved for system use and cannot be used for user-defined types.
/// </summary>
internal static class ReservedTypeNames
{
    #region Static Fields

    /// <summary>
    /// The reserved type name for event records.
    /// </summary>
    internal static readonly string Event = "event";

    // Array containing all reserved type names for validation
    private static readonly string[] _reservedTypeNames = [ Event ];

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines whether the specified type name is reserved for system use.
    /// </summary>
    /// <param name="typeName">The type name to check.</param>
    /// <returns>True if the type name is reserved; otherwise, false.</returns>
    public static bool IsReserved(
        string typeName)
    {
        return _reservedTypeNames.Any(rtn => string.Equals(rtn, typeName));
    }

    #endregion
}
