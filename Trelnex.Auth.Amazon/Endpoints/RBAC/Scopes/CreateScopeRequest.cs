using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to create a new scope for a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to define a new scope within the context of a protected resource.
/// Scopes in RBAC represent authorization boundaries that limit the context in which a resource
/// can be accessed. Common examples of scopes include environments (dev, test, prod),
/// geographical regions, or logical domains. Creating a scope is typically done during
/// the initial setup of a resource's authorization model to segregate access across
/// different environments or contexts.
/// </remarks>
public record CreateScopeRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which to create the scope.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the scope is being defined.
    /// The resource must exist in the system before scopes can be created for it.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope to create.
    /// </summary>
    /// <remarks>
    /// The scope name uniquely identifies this authorization boundary within the resource.
    /// Common scope names might include environment designations (e.g., "development", "production"),
    /// geographical regions (e.g., "us-west", "eu-central"), or logical domains.
    /// Scope names must follow the validation rules defined in the system,
    /// typically requiring alphanumeric characters with limited special characters.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    #endregion
}
