using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a role definition for a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// In Role-Based Access Control (RBAC), a role defines a collection of permissions that
/// can be assigned to principals (users, services, or other identities) for a specific resource.
///
/// Roles serve as an abstraction layer between principals and permissions, allowing administrators
/// to define standardized permission sets and assign them to multiple principals. This simplifies
/// permission management by grouping related permissions together and enabling consistent
/// access control across the system.
///
/// Each role is defined in the context of a specific resource and can only be assigned
/// to principals for that resource. Common examples of roles include "Reader", "Contributor",
/// and "Administrator", each granting progressively more permissions on the resource.
/// </remarks>
internal class Role
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which this role applies.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset (API, service, data, etc.) for which
    /// this role is defined. Roles are resource-specific, meaning they can only be assigned
    /// within the context of the specified resource.
    ///
    /// Together with the role name, the resource name forms a composite key that uniquely
    /// identifies this role within the RBAC system.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the unique name of the role within the resource context.
    /// </summary>
    /// <remarks>
    /// The role name identifies a specific set of permissions that can be granted for the resource.
    /// Role names should be descriptive of the access level or function they provide, following
    /// a consistent naming convention across the system.
    ///
    /// When a role is assigned to a principal (via a principal membership), the principal
    /// gains all the permissions defined by that role for the specified resource, within
    /// the constraints of any assigned scopes.
    /// </remarks>
    [JsonPropertyName("roleName")]
    public string RoleName { get; init; } = null!;

    #endregion
}
