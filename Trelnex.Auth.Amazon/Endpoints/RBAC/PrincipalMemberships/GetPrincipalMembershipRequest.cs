using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve principal membership information from the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to query the roles assigned to a principal (user or service)
/// for a specific resource and optional scope. The response will contain the roles
/// and scopes the principal has access to within the specified context.
/// </remarks>
public record GetPrincipalMembershipRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the identifier of the principal (user or service) whose membership information is being requested.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    /// <summary>
    /// Gets the name of the resource for which to retrieve the principal's role memberships.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system.
    /// This is typically an API or service name that the principal is trying to access.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    /// <summary>
    /// Gets the optional name of the scope for which to retrieve the principal's role memberships.
    /// </summary>
    /// <remarks>
    /// The scope name defines the authorization boundary for the roles. If specified,
    /// only roles assigned within this scope will be returned. If null, roles across all
    /// scopes may be returned, depending on the implementation.
    ///
    /// Scopes are typically used to represent environments (dev/test/prod) or other
    /// authorization boundaries.
    /// </remarks>
    [JsonPropertyName("scopeName")]
    [SwaggerSchema("The optional name of the scope.")]
    public required string? ScopeName { get; init; }

    #endregion
}
