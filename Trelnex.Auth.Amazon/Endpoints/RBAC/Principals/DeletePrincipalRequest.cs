using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to delete a principal from the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to completely remove a principal (user or service) and all of its
/// associated role assignments from the RBAC system. This operation is typically performed
/// when a principal no longer needs access to any resources or when it has been deprovisioned
/// from the identity system. The operation cascades to remove all role assignments for this
/// principal across all resources and scopes.
/// </remarks>
public record DeletePrincipalRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the identifier of the principal (user or service) to be deleted.
    /// </summary>
    /// <remarks>
    /// The principal ID typically represents an AWS IAM Role ARN, IAM User ARN, or other
    /// identity provider's unique identifier for the entity. Once deleted, all role
    /// assignments for this principal will be removed, effectively revoking all access
    /// permissions across all resources and scopes.
    /// </remarks>
    [JsonPropertyName("principalId")]
    [SwaggerSchema("The principal id.", Nullable = false)]
    public required string PrincipalId { get; init; }

    #endregion
}
