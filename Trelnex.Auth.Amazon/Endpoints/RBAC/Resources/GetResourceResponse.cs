using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a response containing information about a resource in the RBAC system.
/// </summary>
/// <remarks>
/// This response provides comprehensive information about a resource in the Role-Based
/// Access Control system, including its name and the scopes and roles that have been
/// defined for it. This information is useful for administrative purposes, auditing,
/// and understanding the authorization model for a specific resource.
/// </remarks>
public record GetResourceResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource.
    /// </summary>
    /// <remarks>
    /// The resource name serves as the primary identifier for the resource within the RBAC system.
    /// It is used when granting roles to principals and when making authorization decisions.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the collection of all scopes defined for the resource.
    /// </summary>
    /// <remarks>
    /// Scopes define authorization boundaries within which role assignments can be made.
    /// Common examples of scopes include environments (e.g., "dev", "test", "prod") or
    /// geographical regions. This property provides a complete list of all scopes that
    /// have been defined for this resource, allowing administrators to understand the
    /// authorization boundaries in place.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    [SwaggerSchema("The array of available scopes for the resource.", Nullable = false)]
    public required string[] ScopeNames { get; init; }

    /// <summary>
    /// Gets the collection of all roles defined for the resource.
    /// </summary>
    /// <remarks>
    /// Roles define sets of permissions that can be granted to principals. Common examples
    /// of roles include "Reader", "Writer", or "Administrator". This property provides a
    /// complete list of all roles that have been defined for this resource, allowing
    /// administrators to understand the permission model in place.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    [SwaggerSchema("The array of available roles for the resource.", Nullable = false)]
    public required string[] RoleNames { get; init; }

    #endregion
}
