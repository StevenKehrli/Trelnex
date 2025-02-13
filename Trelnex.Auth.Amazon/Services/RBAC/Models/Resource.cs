using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a specific resource.
/// </summary>
internal class Resource
{
    /// <summary>
    /// The name of the resource.
    /// </summary>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// The collection of all available scopes for the resource.
    /// </summary>
    [JsonPropertyName("scopeNames")]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// The collection of all available roles for the resource.
    /// </summary>
    [JsonPropertyName("roleName")]
    public string[] RoleNames { get; init; } = null!;
}
