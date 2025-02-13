using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

public record GetRoleAssignmentResponse
{
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    [JsonPropertyName("principalIds")]
    [SwaggerSchema("The array of principal ids assigned to the role.", Nullable = false)]
    public required string[] PrincipalIds { get; init; }
}
