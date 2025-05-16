using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents an authorization boundary (scope) for a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// In Role-Based Access Control (RBAC), a scope defines a context or boundary within which
/// access permissions are valid. Scopes add an additional dimension to the RBAC model, allowing
/// permissions to be limited to specific environments, regions, or domains.
///
/// Scopes serve several important purposes in the RBAC system:
///
/// - They enable fine-grained control by limiting where roles can be exercised
/// - They support multi-tenancy and isolation between different environments
/// - They allow for progressive deployment of permissions across different contexts
///
/// Common examples of scopes include:
/// - Environment tiers (development, testing, production)
/// - Geographical regions (us-east, eu-west)
/// - Logical domains or business units
///
/// Each scope is defined in the context of a specific resource and can be assigned
/// to principals when granting them roles for that resource.
/// </remarks>
internal class Scope
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which this scope applies.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset (API, service, data, etc.) for which
    /// this scope is defined. Scopes are resource-specific, meaning they are only valid within
    /// the context of the specified resource.
    ///
    /// Together with the scope name, the resource name forms a composite key that uniquely
    /// identifies this scope within the RBAC system.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the unique name of the scope within the resource context.
    /// </summary>
    /// <remarks>
    /// The scope name identifies a specific authorization boundary for the resource.
    /// Scope names should be descriptive of the context they represent, following
    /// a consistent naming convention across the system.
    ///
    /// When a principal is granted roles for a resource, the assignment includes specific
    /// scopes to define the boundaries within which those roles can be exercised. If a
    /// principal attempts to access a resource outside of their assigned scopes, the
    /// access will be denied even if they have the appropriate roles.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    public string ScopeName { get; init; } = null!;

    #endregion
}
