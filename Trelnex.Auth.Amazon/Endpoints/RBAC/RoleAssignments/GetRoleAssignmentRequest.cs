using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve information about all principals assigned to a specific role for a resource.
/// </summary>
/// <remarks>
/// This request is used to query the role assignment information in the RBAC system,
/// providing details about which principals (users or services) have been granted a specific role
/// for a particular resource. This operation is typically used for administrative purposes, auditing,
/// and understanding the current authorization state for a resource-role combination.
/// </remarks>
public record GetRoleAssignmentRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which to retrieve role assignments.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which you want to see all principals
    /// that have been assigned a specific role.
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role for which to retrieve assignments.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Common examples include "Reader", "Writer", or "Administrator".
    /// This request will return all principals that have been assigned this role
    /// for the specified resource, potentially across different scopes.
    /// </remarks>
    [FromQuery(Name = "roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
