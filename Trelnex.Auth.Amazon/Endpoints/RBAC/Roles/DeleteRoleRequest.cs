using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to delete a role from a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to remove a role from a protected resource in the Role-Based Access Control system.
/// When a role is deleted, all assignments of this role to principals are also removed, effectively
/// revoking this specific set of permissions from all users or services that had been granted it.
/// This operation is typically performed during cleanup, removal of deprecated access levels,
/// or when restructuring the authorization model for a resource.
/// </remarks>
public record DeleteRoleRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource from which to delete the role.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the role is being removed.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role to delete.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Deleting a role is a cascading operation that will remove all assignments of this role
    /// to principals across all scopes, effectively revoking these permissions from all users
    /// or services that had been granted it.
    /// </remarks>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
