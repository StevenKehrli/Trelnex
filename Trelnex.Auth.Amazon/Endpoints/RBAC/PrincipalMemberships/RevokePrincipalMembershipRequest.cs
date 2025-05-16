using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to revoke a role from a principal for a specific resource.
/// </summary>
/// <remarks>
/// This request is used to remove a role assignment in the RBAC system, effectively
/// revoking specific permissions from a principal (user or service) on a resource.
/// After successful revocation, the principal will no longer have the permissions
/// associated with the specified role for the resource.
/// </remarks>
public record RevokePrincipalMembershipRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the identifier of the principal (user or service) from whom the role should be revoked.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity. This is the subject that will
    /// have the specified permissions removed.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    /// <summary>
    /// Gets the name of the resource for which the role is being revoked.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the principal's access
    /// is being modified.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the role to revoke from the principal.
    /// </summary>
    /// <remarks>
    /// The role name identifies a set of permissions in the RBAC system.
    /// After successful revocation, the principal will no longer have these
    /// permissions for the specified resource. Revoking a role that was not
    /// previously granted is typically treated as a no-op.
    /// </remarks>
    [JsonPropertyName("roleName")]
    [SwaggerSchema("The name of the role.", Nullable = false)]
    public required string RoleName { get; init; }

    #endregion
}
