using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a response containing information about a specific role for a resource in the RBAC system.
/// </summary>
/// <remarks>
/// This response provides details about a role defined for a protected resource in the RBAC system.
/// It includes the basic identifying information for the role, such as its name and the resource
/// it belongs to. This information is useful for administrators and management tools to inspect
/// and validate role definitions as part of security auditing and authorization model maintenance.
/// </remarks>
public record GetRoleResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which the role belongs.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the role is defined.
    /// Each role exists within the context of a specific resource.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Common examples include "Reader", "Writer", or "Administrator".
    /// The role name is unique within the context of a resource and serves
    /// as the identifier used when granting these permissions to principals.
    /// </remarks>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
