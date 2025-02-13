using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a scope for a specific resource.
/// </summary>
internal class Scope
{
    /// <summary>
    /// The name of the resource.
    /// </summary>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// The name of the scope.
    /// </summary>
    [JsonPropertyName("scopeName")]
    public string ScopeName { get; init; } = null!;
}
