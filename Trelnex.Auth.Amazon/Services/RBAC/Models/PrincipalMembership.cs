using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents the roles assigned to a principal within a specific resource.
/// </summary>
internal class PrincipalMembership
{
    /// <summary>
    /// Gets the unique id of the principal
    /// </summary>
    [JsonPropertyName("principalId")]
    public string PrincipalId { get; init; } = null!;

    /// <summary>
    /// The name of the resource.
    /// </summary>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the names of the scopes assigned to the principal for the specified resource.
    /// </summary>
    [JsonPropertyName("scopeNames")]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the names of the roles assigned to the principal for the specified resource.
    /// </summary>
    [JsonPropertyName("roleNames")]
    public string[] RoleNames { get; init; } = null!;
}
