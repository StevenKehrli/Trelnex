using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents the complete access profile for a principal on a specific resource, including assigned roles and scopes.
/// </summary>
/// <remarks>
/// This class encapsulates all access information for a principal (user or service) within the context
/// of a specific resource in the RBAC system. It provides a comprehensive view of what permissions
/// and scope boundaries the principal has been granted, enabling authorization decisions and
/// administrative oversight of access control assignments.
/// </remarks>
internal class PrincipalAccess
{
    /// <summary>
    /// Gets the unique identifier of the principal who has been granted access.
    /// </summary>
    /// <value>
    /// The principal ID, typically an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity.
    /// </value>
    /// <remarks>
    /// This identifier uniquely represents the subject (user or service) that has
    /// been granted the roles and scopes specified in this access profile.
    /// </remarks>
    [JsonPropertyName("principalId")]
    public string PrincipalId { get; init; } = null!;

    /// <summary>
    /// Gets the name of the resource for which the principal has been granted access.
    /// </summary>
    /// <value>
    /// The resource name that identifies the protected asset in the RBAC system,
    /// such as "api://amazon.auth.trelnex.com".
    /// </value>
    /// <remarks>
    /// The resource name serves as the context for all role and scope assignments
    /// contained within this access profile.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the array of scope names assigned to the principal for the specified resource.
    /// </summary>
    /// <value>
    /// An array of scope names that define authorization boundaries for the principal,
    /// such as ["rbac"].
    /// </value>
    /// <remarks>
    /// Scopes define the authorization boundaries within which the principal can operate.
    /// Multiple scopes may be assigned to provide different levels of access across
    /// various organizational or functional boundaries.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the array of role names assigned to the principal for the specified resource.
    /// </summary>
    /// <value>
    /// An array of role names that define the permissions granted to the principal,
    /// such as ["rbac.create", "rbac.read", "rbac.update", "rbac.delete"].
    /// </value>
    /// <remarks>
    /// Roles define the specific permissions and actions the principal is authorized
    /// to perform on the resource. Multiple roles may be assigned to provide
    /// comprehensive access across different functional areas.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    public string[] RoleNames { get; init; } = null!;
}
