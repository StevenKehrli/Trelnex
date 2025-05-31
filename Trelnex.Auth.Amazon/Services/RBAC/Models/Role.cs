using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a role definition for a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// A role defines a specific permission that can be assigned to principals for a particular resource.
/// Roles represent actions or capabilities that principals can perform within the context of a resource,
/// such as create, read, update, or delete operations.
/// </remarks>
internal class Role
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which this role applies.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    /// <remarks>
    /// The resource name provides the context for this role definition.
    /// Roles are always defined within the scope of a specific resource.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the unique name of the role within the resource context.
    /// </summary>
    /// <value>
    /// The role name, such as "rbac.create", "rbac.read", "rbac.update", or "rbac.delete".
    /// </value>
    /// <remarks>
    /// The role name uniquely identifies the permission within the resource.
    /// Role names typically follow a hierarchical naming convention that reflects
    /// the functional area and specific action or capability.
    /// </remarks>
    [JsonPropertyName("roleName")]
    public string RoleName { get; init; } = null!;

    #endregion
}
