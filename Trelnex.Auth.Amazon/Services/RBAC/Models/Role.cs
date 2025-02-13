using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a role for a specific resource.
/// </summary>
internal class Role
{
    /// <summary>
    /// The name of the resource.
    /// </summary>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// The name of the role.
    /// </summary>
    [JsonPropertyName("roleName")]
    public string RoleName { get; init; } = null!;
}
