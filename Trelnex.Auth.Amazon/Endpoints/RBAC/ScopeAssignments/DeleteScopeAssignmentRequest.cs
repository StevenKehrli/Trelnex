using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to delete a scope assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This request is used to remove a scope assignment in the RBAC system, effectively
/// removing specific scope boundaries from a principal (user or service) on a resource.
/// After successful deletion, the principal will no longer have the scope boundaries
/// associated with the specified scope for the resource.
/// </remarks>
public record DeleteScopeAssignmentRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the scope assignment is being deleted.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name for which the principal's access
    /// is being modified.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope assignment to delete for the principal.
    /// </summary>
    /// <remarks>
    /// The scope name identifies authorization boundaries in the RBAC system.
    /// After successful deletion, the principal will no longer have these
    /// scope boundaries for the specified resource. Deleting a scope assignment that was not
    /// previously created is typically treated as a no-op.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    /// <summary>
    /// Gets the identifier of the principal (user or service) for whom the scope assignment should be deleted.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity. This is the subject that will
    /// have the specified scope boundaries removed.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    #endregion
}
