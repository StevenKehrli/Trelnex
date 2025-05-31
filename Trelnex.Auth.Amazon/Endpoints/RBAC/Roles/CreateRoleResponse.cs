using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a response confirming the successful creation of a role for a resource in the RBAC system.
/// </summary>
/// <remarks>
/// This response is returned when a new role has been successfully created for a protected resource.
/// It confirms the resource and role names, serving as validation that the role is now available
/// for assignment to principals. The response is intentionally minimal, containing just enough
/// information to confirm the identity of the newly created role.
/// </remarks>
public record CreateRoleResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the role was created.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This property echoes back the resource name that was provided in the request,
    /// confirming that the role was created in the context of this resource.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role that was created.
    /// </summary>
    /// <remarks>
    /// This property echoes back the role name that was provided in the request,
    /// confirming that the role was successfully created with this identifier.
    /// The role name identifies a set of permissions in the RBAC system that can
    /// now be assigned to principals to grant them specific access rights.
    /// </remarks>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
