using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents the response returned after successfully retrieving scope information in the RBAC system.
/// </summary>
/// <remarks>
/// This response contains information about a specific scope within a protected resource.
/// It provides the core identifying details of the scope, confirming its existence in the system.
/// This information is typically used in administrative interfaces or during programmatic
/// interactions with the RBAC system to verify scope existence or display scope information.
///
/// If the requested scope does not exist, the endpoint will return a 404 Not Found response
/// instead of this response object.
/// </remarks>
public record GetScopeResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource to which the scope belongs.
    /// </summary>
    /// <remarks>
    /// This property identifies the protected asset in the RBAC system that contains this scope.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope being described.
    /// </summary>
    /// <remarks>
    /// This property represents the identifier for the authorization boundary,
    /// which can be used in role assignments to limit the context in which roles apply.
    /// Common scope names include environment designations like "development" or "production",
    /// or other logical authorization boundaries.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    #endregion
}
