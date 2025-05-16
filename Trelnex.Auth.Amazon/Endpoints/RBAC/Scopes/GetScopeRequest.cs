using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve information about a specific scope in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to query details about a scope that exists within a protected resource.
/// Scope information is essential for understanding the authorization boundaries of a resource
/// and for managing role assignments that are limited to specific scopes. This request
/// is typically used in administrative interfaces that display or manage RBAC configurations,
/// or during programmatic interactions with the RBAC system.
///
/// Common use cases include:
/// - Verifying the existence of a scope before creating role assignments
/// - Retrieving scope information for display in management interfaces
/// - Auditing the authorization boundaries configured for a resource
/// </remarks>
public record GetScopeRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which to retrieve scope information.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name containing the scope being queried.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope to retrieve.
    /// </summary>
    /// <remarks>
    /// The scope name identifies the specific authorization boundary to query.
    /// This can represent environments (e.g., "development", "production"),
    /// geographical regions, or other logical authorization boundaries.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    #endregion
}
