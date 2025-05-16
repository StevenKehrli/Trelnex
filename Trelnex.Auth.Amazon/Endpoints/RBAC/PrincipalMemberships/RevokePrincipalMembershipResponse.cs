using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents the response after revoking a role from a principal for a specific resource.
/// </summary>
/// <remarks>
/// This response reflects the updated state of a principal's memberships after a role
/// has been revoked. It includes the complete list of roles and scopes the principal still
/// has access to for the specified resource, allowing clients to confirm the changes
/// and understand the principal's updated permissions.
/// </remarks>
public record RevokePrincipalMembershipResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the identifier of the principal (user or service) from whom the role was revoked.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public string PrincipalId { get; init; } = null!;

    /// <summary>
    /// Gets the name of the resource for which the role was revoked.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the principal's access
    /// has been modified.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the array of scope names within which the principal still has role assignments.
    /// </summary>
    /// <remarks>
    /// Scopes define authorization boundaries for role assignments. This array includes
    /// all scopes in which the principal has roles after the revoke operation. If the
    /// revoked role was the only role in a particular scope, that scope may no longer
    /// appear in this array.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    [SwaggerSchema("The array of scopes assigned to the principal.", Nullable = false)]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the array of role names still assigned to the principal for the specified resource.
    /// </summary>
    /// <remarks>
    /// This array includes all roles assigned to the principal after the revoke operation,
    /// which should no longer include the revoked role. This provides a complete view of
    /// the principal's remaining permissions on the resource.
    ///
    /// If the principal had no other roles besides the one that was revoked, this may be an empty array.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    [SwaggerSchema("The array of roles assigned to the principal.", Nullable = false)]
    public string[] RoleNames { get; init; } = null!;

    #endregion
}
