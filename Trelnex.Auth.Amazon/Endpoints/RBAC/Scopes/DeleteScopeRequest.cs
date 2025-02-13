using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

public record DeleteScopeRequest
{
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }
}
