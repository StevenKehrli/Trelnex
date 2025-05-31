using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a response confirming the successful creation of a resource in the RBAC system.
/// </summary>
/// <remarks>
/// This response is returned when a new resource has been successfully registered in the
/// Role-Based Access Control system. It confirms the name of the resource that was created
/// and serves as validation that the resource is now available for role and scope definition.
/// The response is intentionally minimal, containing just the resource name to confirm identity.
/// </remarks>
public record CreateResourceResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource that was created.
    /// </summary>
    /// <remarks>
    /// This property echoes back the resource name that was provided in the request,
    /// confirming that the resource was successfully created with this identifier.
    /// The resource name serves as the primary key for the resource entity in the RBAC system.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    #endregion
}
