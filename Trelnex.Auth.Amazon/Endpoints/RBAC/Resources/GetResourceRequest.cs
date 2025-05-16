using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to retrieve information about a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to obtain details about a resource (such as an API or service) that
/// has been registered in the Role-Based Access Control system. The response includes information
/// about the resource, such as its name and any metadata associated with it. This operation
/// is typically used for administrative purposes or to confirm the existence of a resource
/// before performing operations that depend on it.
/// </remarks>
public record GetResourceRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource to retrieve.
    /// </summary>
    /// <remarks>
    /// The resource name identifies the protected asset whose information should be retrieved
    /// from the RBAC system. This is the primary key used to locate the resource entity.
    /// If no resource with this name exists, a 404 Not Found response will typically be returned.
    /// </remarks>
    [JsonPropertyName("resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    #endregion
}
