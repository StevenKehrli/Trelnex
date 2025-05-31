using System.Text.Json.Serialization;

namespace Trelnex.Core.Identity;

/// <summary>
/// Represents the health status of an access token.
/// </summary>
/// <remarks>
/// Used for diagnostics and monitoring of token validity.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessTokenHealth
{
    /// <summary>
    /// Indicates that the access token has expired and is no longer valid.
    /// </summary>
    Expired = 0,

    /// <summary>
    /// Indicates that the access token is currently valid and usable.
    /// </summary>
    Valid = 1,
}
