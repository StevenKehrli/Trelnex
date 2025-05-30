using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents the response after creating a role assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This response reflects the updated state of a principal's role assignments after a new
/// assignment has been created. It includes the complete list of roles and scopes the principal now
/// has access to for the specified resource, allowing clients to confirm the changes
/// and understand the principal's updated permissions.
/// </remarks>
public record CreateRoleAssignmentResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the role assignment has been created.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name that the principal now has access to.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the identifier of the principal (user or service) for whom the role assignment has been created.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public string PrincipalId { get; init; } = null!;

    /// <summary>
    /// Gets the array of scope names within which the principal has role assignments.
    /// </summary>
    /// <remarks>
    /// Scopes define authorization boundaries for role assignments. This array includes
    /// all scopes in which the principal has roles after the create operation, which may
    /// include both pre-existing scopes and any new scope added by the create operation.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    [SwaggerSchema("The array of scopes assigned to the principal.", Nullable = false)]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the array of role names now assigned to the principal for the specified resource.
    /// </summary>
    /// <remarks>
    /// This array includes all roles assigned to the principal after the create operation,
    /// which includes both pre-existing roles and the newly created role assignment. This provides
    /// a complete view of the principal's permissions on the resource.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    [SwaggerSchema("The array of roles assigned to the principal.", Nullable = false)]
    public string[] RoleNames { get; init; } = null!;

    #endregion
}
