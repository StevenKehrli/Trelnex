using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

public record GetRoleRequest
{
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }
}
