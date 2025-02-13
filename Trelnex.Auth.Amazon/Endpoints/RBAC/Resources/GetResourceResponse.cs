using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

public record GetResourceResponse
{
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// The collection of all available scopes for the resource.
    /// </summary>
    [JsonPropertyName("scopeNames")]
    [SwaggerSchema("The array of available scopes for the resource.", Nullable = false)]
    public required string[] ScopeNames { get; init; }

    /// <summary>
    /// The collection of all available roles for the resource.
    /// </summary>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The array of available roles for the resource.", Nullable = false)]
    public required string[] RoleNames { get; init; }
}
