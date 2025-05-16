using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a protected resource in the RBAC system with its associated roles and scopes.
/// </summary>
/// <remarks>
/// In Role-Based Access Control (RBAC), a resource is an asset, API, service, or data
/// that requires access control. Each resource is configured with:
///
/// - A unique identifying name
/// - A set of scopes (authorization boundaries) in which access can be granted
/// - A set of roles defining the different permission levels available for the resource
///
/// Resources serve as the foundation of the RBAC system, defining what is being protected
/// and the available permission structures. Resources must be defined before roles can be
/// created or principals can be granted access to them.
/// </remarks>
internal class Resource
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource.
    /// </summary>
    /// <remarks>
    /// The resource name serves as the primary identifier for a protected asset within the RBAC system.
    /// This is typically an API name, service identifier, or other unique string that identifies
    /// what is being protected. The name should be concise, descriptive, and follow a consistent
    /// naming convention across the organization.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the collection of all available scopes defined for this resource.
    /// </summary>
    /// <remarks>
    /// Scopes represent the authorization boundaries or contexts in which access to the resource
    /// can be granted. Common examples include environments (dev, test, prod), geographical regions,
    /// or logical domains.
    ///
    /// Each scope represents a distinct authorization boundary within which role assignments can be made.
    /// When a principal is granted access to a resource, specific scopes must be selected to define
    /// the contexts in which that access is valid.
    /// </remarks>
    [JsonPropertyName("scopeNames")]
    public string[] ScopeNames { get; init; } = null!;

    /// <summary>
    /// Gets the collection of all available roles defined for this resource.
    /// </summary>
    /// <remarks>
    /// Roles represent distinct permission sets that can be assigned to principals for this resource.
    /// Each role typically corresponds to a specific function or permission level (e.g., "Reader",
    /// "Contributor", "Administrator").
    ///
    /// Roles are defined at the resource level, meaning each resource can have its own set of
    /// roles tailored to its specific access control requirements. When granting access to principals,
    /// one or more of these roles can be assigned within the context of selected scopes.
    /// </remarks>
    [JsonPropertyName("roleName")]
    public string[] RoleNames { get; init; } = null!;

    #endregion
}
