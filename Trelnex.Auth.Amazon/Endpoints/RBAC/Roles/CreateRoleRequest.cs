using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to create a new role for a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to define a new role within the context of a protected resource.
/// Roles in RBAC represent collections of permissions that can be assigned to principals.
/// Creating a role is a prerequisite before it can be granted to principals, giving them
/// specific access rights to the resource. Roles are typically created during the initial
/// setup of a resource's authorization model.
/// </remarks>
public record CreateRoleRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which to create the role.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the role is being defined.
    /// The resource must exist in the system before roles can be created for it.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role to create.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Common examples include "Reader", "Writer", or "Administrator".
    /// Role names should be descriptive and reflect the level of access they provide.
    /// Role names must be unique within a resource context.
    /// </remarks>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
