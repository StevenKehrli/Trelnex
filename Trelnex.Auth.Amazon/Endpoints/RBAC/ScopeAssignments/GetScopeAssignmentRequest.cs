using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve information about all principals assigned to a specific scope for a resource.
/// </summary>
/// <remarks>
/// This request is used to query the scope assignment information in the RBAC system,
/// providing details about which principals (users or services) have been granted a specific scope
/// for a particular resource. This operation is typically used for administrative purposes, auditing,
/// and understanding the current authorization state for a resource-scope combination.
/// </remarks>
public record GetScopeAssignmentRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which to retrieve scope assignments.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which you want to see all principals
    /// that have been assigned a specific scope.
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope for which to retrieve assignments.
    /// </summary>
    /// <remarks>
    /// The scope name identifies authorization boundaries in the RBAC system.
    /// Common examples include "Global", "Department", or "Project".
    /// This request will return all principals that have been assigned this scope
    /// for the specified resource.
    /// </remarks>
    [FromQuery(Name = "scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    #endregion
}
