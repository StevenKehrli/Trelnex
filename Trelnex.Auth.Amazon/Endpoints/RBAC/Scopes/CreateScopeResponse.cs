using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents the response returned after successfully creating a scope in the RBAC system.
/// </summary>
/// <remarks>
/// This response confirms the successful creation of a scope for a specific resource.
/// It echoes back the resource name and scope name that were provided in the creation request,
/// serving as confirmation that the scope was successfully created in the system.
/// This response is returned by the create scope endpoint when the operation completes successfully.
/// </remarks>
public record CreateScopeResponse
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the scope was created.
    /// </summary>
    /// <remarks>
    /// This property reflects the resource name provided in the original request,
    /// confirming which protected asset the new scope belongs to.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the newly created scope.
    /// </summary>
    /// <remarks>
    /// This property reflects the scope name provided in the original request,
    /// confirming the identifier of the newly created authorization boundary.
    /// Once created, this scope can be referenced in role assignments to limit
    /// the context in which those roles apply.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    #endregion
}
