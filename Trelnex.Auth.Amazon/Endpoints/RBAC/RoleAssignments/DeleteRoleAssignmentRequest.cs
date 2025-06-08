using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to delete a role assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This request is used to remove a role assignment in the RBAC system, effectively
/// removing specific permissions from a principal (user or service) on a resource.
/// After successful deletion, the principal will no longer have the permissions
/// associated with the specified role for the resource.
/// </remarks>
public record DeleteRoleAssignmentRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the role assignment is being deleted.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the principal's access
    /// is being modified.
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role assignment to delete for the principal.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// After successful deletion, the principal will no longer have these
    /// permissions for the specified resource. Deleting a role assignment that was not
    /// previously created is typically treated as a no-op.
    /// </remarks>
    [FromQuery(Name = "roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    /// <summary>
    /// Gets the identifier of the principal (user or service) for whom the role assignment should be deleted.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity. This is the subject that will
    /// have the specified permissions removed.
    /// </remarks>
    [FromQuery(Name = "principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    #endregion
}
