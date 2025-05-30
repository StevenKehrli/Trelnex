using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to create a scope assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This request is used to create a scope assignment in the RBAC system, giving
/// a principal (user or service) specific scope boundaries on a resource. The scope defines
/// the authorization boundary within which the principal can operate, and the assignment creates the authorization
/// relationship between the principal, resource, and scope.
/// </remarks>
public record CreateScopeAssignmentRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the name of the resource for which the scope assignment is being created.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name that the principal needs to access.
    /// The resource must exist in the system before scope assignments can be created for it.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the scope to assign to the principal.
    /// </summary>
    /// <remarks>
    /// The scope name identifies authorization boundaries in the RBAC system.
    /// Common examples include "Global", "Department", or "Project".
    /// The scope must exist for the specified resource before it can be assigned.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The name of the scope.", Nullable = false)]
    public required string ScopeName { get; init; }

    /// <summary>
    /// Gets the identifier of the principal (user or service) for whom the scope assignment should be created.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity. This is the subject that will
    /// receive the scope boundaries defined by the scope.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    #endregion
}
