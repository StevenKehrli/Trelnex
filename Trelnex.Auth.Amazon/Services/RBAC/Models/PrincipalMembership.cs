using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents the roles and scopes assigned to a principal within a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// In Role-Based Access Control (RBAC), a principal membership defines the relationship between:
/// - A principal (user, service, or identity)
/// - A resource (protected API or service)
/// - The roles granted to the principal for that resource
/// - The scopes (authorization boundaries) within which those roles apply
///
/// This entity is used to track and enforce access control decisions by aggregating
/// all authorizations granted to a specific principal for a resource. It supports the
/// implementation of fine-grained permission models where access depends on both the
/// assigned roles and the contexts (scopes) in which those roles are valid.
/// </remarks>
internal class PrincipalMembership
{
    #region Public Properties

    /// <summary>
    /// Gets the unique identifier of the principal.
    /// </summary>
    /// <remarks>
    /// The principal ID identifies the entity (user, service, or other identity) that
    /// has been granted access to the resource. This is typically an AWS ARN, email address,
    /// UUID, or other unique identifier that can be authenticated by the system.
    /// </remarks>
    [JsonPropertyName("principalId")]
    public string PrincipalId { get; init; } = null!;

    /// <summary>
    /// Gets the name of the resource to which the principal has been granted access.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset (API, service, data, etc.) for which
    /// the principal has been granted specific roles. The combination of principal ID and
    /// resource name forms a unique key for identifying principal memberships.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the names of the scopes that define the authorization boundaries for the principal's roles.
    /// </summary>
    /// <remarks>
    /// Scopes represent the contexts or boundaries within which the assigned roles are valid.
    /// Common examples include environments (dev, test, prod), geographical regions, or logical domains.
    ///
    /// A principal must have at least one scope assigned for their roles to be effective.
    /// If multiple scopes are assigned, the principal can exercise their roles within any of those scopes.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the names of the roles assigned to the principal for the specified resource.
    /// </summary>
    /// <remarks>
    /// Roles represent collections of permissions that define what actions the principal
    /// can perform on the resource. Multiple roles can be assigned to a principal to provide
    /// the complete set of permissions needed for their intended activities.
    ///
    /// When evaluating permissions, the system considers the union of all permissions granted
    /// by each assigned role, within the constraints of the assigned scopes.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    public string[] RoleNames { get; init; } = null!;

    #endregion
}
