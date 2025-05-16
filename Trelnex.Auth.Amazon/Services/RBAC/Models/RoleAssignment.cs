using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a collection of principals assigned to a specific role for a resource in the RBAC system.
/// </summary>
/// <remarks>
/// In Role-Based Access Control (RBAC), a role assignment tracks which principals (users, services,
/// or other identities) have been granted a particular role for a specific resource. This class
/// provides a role-centric view of access control, showing all principals that have been granted
/// a specific permission set.
///
/// While the <see cref="PrincipalMembership"/> class offers a principal-centric view (showing all roles
/// assigned to a principal), the RoleAssignment class offers the inverse relationship - a role-centric view
/// showing all principals with that role. Both perspectives are useful for different administrative
/// and auditing purposes.
///
/// This entity is primarily used for administrative interfaces, reporting, and auditing to answer
/// questions like "Who has access to this role?" or "How widely is this role distributed?".
/// </remarks>
internal class RoleAssignment
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which this role assignment exists.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset (API, service, data, etc.) that
    /// this role assignment applies to. The combination of resource name and role name
    /// forms a unique key for identifying specific role assignments.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the name of the role that has been assigned to the principals.
    /// </summary>
    /// <remarks>
    /// The role name identifies the specific set of permissions that have been granted
    /// to the listed principals for the specified resource. This representation allows
    /// for tracking who has been assigned specific permission sets, which is valuable
    /// for security auditing and access control management.
    /// </remarks>
    [JsonPropertyName("roleName")]
    public string RoleName { get; init; } = null!;

    /// <summary>
    /// Gets an array of principal identifiers that have been assigned this role.
    /// </summary>
    /// <remarks>
    /// This collection represents all the entities (users, services, or other identities)
    /// that have been granted this specific role for the resource. Each principal ID typically
    /// corresponds to an AWS ARN, email address, UUID, or other unique identifier that
    /// can be authenticated by the system.
    ///
    /// Note that this list includes all principals regardless of what scope constraints might
    /// be applied to their role assignments. The scope constraints would be defined in each
    /// principal's respective <see cref="PrincipalMembership"/> record.
    /// </remarks>
    [JsonPropertyName("principalIds")]
    public string[] PrincipalIds { get; init; } = null!;

    #endregion
}
