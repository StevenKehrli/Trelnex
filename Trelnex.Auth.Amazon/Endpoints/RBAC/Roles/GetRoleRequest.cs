using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve information about a specific role for a resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to query detailed information about a role defined for a protected resource.
/// It allows administrators and management tools to inspect role definitions, which is useful for
/// auditing, authorization model inspection, and understanding the current security configuration.
/// The response will include details about the role, such as its name and any metadata associated with it.
/// </remarks>
public record GetRoleRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which to retrieve the role information.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the role is defined.
    /// The resource must exist in the system for the role information to be retrieved.
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role to retrieve.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Common examples include "Reader", "Writer", or "Administrator".
    /// This request will return detailed information about the specified role
    /// within the context of the specified resource.
    /// </remarks>
    [FromQuery(Name = "roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
