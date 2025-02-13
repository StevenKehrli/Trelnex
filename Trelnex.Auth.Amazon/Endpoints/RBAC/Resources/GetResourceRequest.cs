using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

public record GetResourceRequest
{
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }
}
