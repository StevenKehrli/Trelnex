using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.RBAC.Models;

/// <summary>
/// Represents a scope definition for a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// A scope defines an authorization boundary within a resource that can be assigned to principals.
/// Scopes allow for fine-grained access control by limiting role assignments to specific contexts
/// or functional areas within a resource.
/// </remarks>
internal class Scope
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which this scope applies.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    /// <remarks>
    /// The resource name provides the context for this scope definition.
    /// Scopes are always defined within the scope of a specific resource.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    public string ResourceName { get; init; } = null!;

    /// <summary>
    /// Gets the name of the scope within the resource.
    /// </summary>
    /// <value>
    /// The scope name, such as "rbac".
    /// </value>
    /// <remarks>
    /// The scope name uniquely identifies the authorization boundary within the resource.
    /// Scope names define the specific context or functional area where access control applies.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    public string ScopeName { get; init; } = null!;

    #endregion
}
