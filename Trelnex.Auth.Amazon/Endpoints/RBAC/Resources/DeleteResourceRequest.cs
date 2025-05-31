using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to delete a resource from the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to completely remove a resource (such as an API or service) and all of its
/// associated roles, scopes, and role assignments from the Role-Based Access Control system.
/// This operation is typically performed when a resource is being decommissioned or is no longer
/// needed for authorization purposes. The operation cascades to remove all roles, scopes, and
/// principal role assignments associated with this resource.
/// </remarks>
public record DeleteResourceRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource to delete.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset that should be removed from the RBAC system.
    /// When a resource is deleted, all roles defined for this resource, all scopes applied to this resource,
    /// and all principal memberships (role assignments) for this resource will also be deleted.
    /// This is a cascading operation that completely removes the resource and its authorization model.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    #endregion
}
