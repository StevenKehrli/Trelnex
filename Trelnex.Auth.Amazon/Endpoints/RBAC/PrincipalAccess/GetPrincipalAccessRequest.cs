using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve principal access information from the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to query the roles assigned to a principal (user or service)
/// for a specific resource and optional scope. The response will contain the roles
/// and scopes the principal has access to within the specified context.
/// </remarks>
public record GetPrincipalAccessRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the identifier of the principal (user or service) whose access information is being requested.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity.
    /// </remarks>
    [FromQuery(Name = "principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    /// <summary>
    /// Gets the name of the resource for which to retrieve the principal's access information.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset in the RBAC system,
    /// such as "api://amazon.auth.trelnex.com".
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    #endregion
}
