namespace Trelnex.Core.Data;

/// <summary>
/// Reserved type names.
/// </summary>
/// <remarks>
/// Prevents naming conflicts between user-defined and system types.
/// </remarks>
internal static class ReservedTypeNames
{
    #region Static Fields

    /// <summary>
    /// Reserved name "event" used to identify event records.
    /// </summary>
    internal static readonly string Event = "event";

    /// <summary>
    /// All reserved type names.
    /// </summary>
    private static readonly string[] _reservedTypeNames = [ Event ];

    #endregion

    #region Public Methods

    /// <summary>
    /// Checks if <paramref name="typeName"/> is reserved.
    /// </summary>
    /// <param name="typeName">Type name to check.</param>
    /// <returns>True if the name is reserved.</returns>
    public static bool IsReserved(
        string typeName)
    {
        return _reservedTypeNames.Any(rtn => string.Equals(rtn, typeName));
    }

    #endregion
}
