using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a response containing information about all principals assigned to a specific role for a resource.
/// </summary>
/// <remarks>
/// This response provides comprehensive information about which principals (users or services)
/// have been granted a specific role for a particular resource in the RBAC system. This information
/// is useful for administrative purposes, auditing, and understanding the current authorization state.
/// It allows administrators to see all entities that have a certain level of access to a resource.
/// </remarks>
public record GetRoleAssignmentResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which role assignments are being returned.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the role assignments are being queried.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role for which assignments are being returned.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// Common examples include "Reader", "Writer", or "Administrator".
    /// This response shows all principals that have been assigned this role
    /// for the specified resource, potentially across different scopes.
    /// </remarks>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    /// <summary>
    /// Gets the array of principal identifiers that have been assigned the role for the resource.
    /// </summary>
    /// <remarks>
    /// Each principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for an entity. These are the subjects that have
    /// been granted the specified role for the specified resource, potentially across different
    /// scopes. An empty array indicates that no principals have been assigned this role.
    /// </remarks>
    [JsonPropertyName("principalIds")]
    [SwaggerSchema("The array of principal ids assigned to the role.", Nullable = false)]
    public required string[] PrincipalIds { get; init; }

    #endregion
}
