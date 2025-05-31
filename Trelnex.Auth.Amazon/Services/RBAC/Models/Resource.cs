using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a protected resource in the RBAC system with its associated roles and scopes.
/// </summary>
/// <remarks>
/// A resource represents any protected asset that requires access control, such as "api://amazon.auth.trelnex.com".
/// Resources define the available scopes and roles that can be assigned to principals for access control.
/// </remarks>
internal class Resource
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    /// <remarks>
    /// The resource name serves as a unique identifier for the protected asset
    /// and is used as the context for all role and scope assignments.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the collection of all available scopes defined for this resource.
    /// </summary>
    /// <value>
    /// An array of scope names, such as ["rbac"].
    /// </value>
    /// <remarks>
    /// Scopes define authorization boundaries that can be assigned to principals.
    /// These are the available scope options for this specific resource.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the collection of all available roles defined for this resource.
    /// </summary>
    /// <value>
    /// An array of role names, such as ["rbac.create", "rbac.read", "rbac.update", "rbac.delete"].
    /// </value>
    /// <remarks>
    /// Roles define specific permissions that can be assigned to principals.
    /// These are the available role options for this specific resource.
    /// </remarks>
    [JsonPropertyName("roleNames")]
    public string[] RoleNames { get; init; } = null!;

    #endregion
}
