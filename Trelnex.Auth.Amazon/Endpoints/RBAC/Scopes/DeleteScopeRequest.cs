using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to delete a scope from a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to remove a scope from a protected resource in the Role-Based Access Control system.
/// When a scope is deleted, all role assignments that reference this scope are also removed,
/// effectively revoking permissions granted within that authorization boundary.
///
/// This operation is typically performed during:
/// - Cleanup of deprecated environments or regions
/// - Reorganization of the authorization model
/// - Removal of testing or temporary authorization boundaries
///
/// Deleting a scope is a significant operation that affects all principals with role assignments
/// in that scope and cannot be undone. Deleted scopes must be recreated if needed again in the future.
/// </remarks>
public record DeleteScopeRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource from which to delete the scope.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name from which the scope will be removed.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope to delete.
    /// </summary>
    /// <remarks>
    /// The scope name identifies the specific authorization boundary to remove from the resource.
    /// Once deleted, all role assignments referencing this scope will no longer be valid,
    /// and principals will lose any permissions granted within this authorization boundary.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    #endregion
}
