namespace Trelnex.Core.Data;

internal static class ReservedTypeNames
{
    internal static readonly string Event = "event";

    private static readonly string[] _reservedTypeNames = [ Event ];

    public static bool IsReserved(
        string typeName)
    {
        return _reservedTypeNames.Any(rtn => string.Equals(rtn, typeName));
    }
}
