using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents the response containing a principal's access information from the RBAC system.
/// </summary>
/// <remarks>
/// This response includes the roles and scopes assigned to a principal (user or service)
/// for a specific resource. This information is used by authorization systems to determine
/// what actions the principal is allowed to perform on the resource.
/// </remarks>
public record GetPrincipalAccessResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which the principal has been granted access.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system,
    /// such as "api://amazon.auth.trelnex.com".
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the identifier of the principal (user or service) whose membership information is being returned.
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
    /// Scopes define authorization boundaries for role assignments, such as "rbac".
    /// The scope names array represents all the authorization contexts in which this principal has roles
    /// for the specified resource.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    [SwaggerSchema("The array of scopes assigned to the principal.", Nullable = false)]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the array of role names assigned to the principal for the specified resource.
    /// </summary>
    /// <remarks>
    /// Roles define sets of permissions that can be performed on a resource, such as
    /// "rbac.create", "rbac.read", "rbac.update", or "rbac.delete". The role names array represents all
    /// the roles (and thus permissions) that this principal has been granted for the
    /// specified resource, potentially across multiple scopes.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    [SwaggerSchema("The array of roles assigned to the principal.", Nullable = false)]
    public string[] RoleNames { get; init; } = null!;

    #endregion
}
