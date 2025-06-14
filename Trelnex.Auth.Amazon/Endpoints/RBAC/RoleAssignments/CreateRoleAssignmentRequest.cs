using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to create a role assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This request is used to create a role assignment in the RBAC system, giving
/// a principal (user or service) specific permissions on a resource. The role defines
/// what actions the principal can perform, and the assignment creates the authorization
/// relationship between the principal, resource, and role.
/// </remarks>
public record CreateRoleAssignmentRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the role assignment is being created.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name that the principal needs to access.
    /// The resource must exist in the system before role assignments can be created for it.
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role to assign to the principal.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Common examples include "Reader", "Writer", or "Administrator".
    /// The role must exist for the specified resource before it can be assigned.
    /// </remarks>
    [FromQuery(Name = "roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    /// <summary>
    /// Gets the identifier of the principal (user or service) for whom the role assignment should be created.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity. This is the subject that will
    /// receive the permissions defined by the role.
    /// </remarks>
    [FromQuery(Name = "principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    #endregion
}
