using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a role assignment for a specific resource.
/// </summary>
internal class RoleAssignment
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

    /// <summary>
    /// Gets an array of principal ids assigned to this role.
    /// </summary>
    [JsonPropertyName("principalIds")]
    public string[] PrincipalIds { get; init; } = null!;
}
