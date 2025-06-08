using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Represents a request to create a new resource in the RBAC system.
/// </summary>
/// <remarks>
/// This request is used to register a new protected resource (such as an API or service)
/// in the Role-Based Access Control system. Resources are the assets that principals
/// can be granted access to through role assignments. Creating a resource is typically
/// the first step in establishing an RBAC model for a given API or service.
/// </remarks>
public record CreateResourceRequest
{
    #region Public Properties

    /// <summary>
    /// Gets the unique name of the resource to create.
    /// </summary>
    /// <remarks>
    /// The resource name serves as a unique identifier for the resource within the RBAC system.
    /// It is used when granting roles to principals and when making authorization decisions.
    /// Resource names should be descriptive and consistent with the naming convention of the
    /// application or service they represent (e.g., "my-api", "data-service", "billing-system").
    /// </remarks>
    [FromQuery(Name = "resourceName")]
    [SwaggerSchema("The name of the resource.", Nullable = false)]
    public required string ResourceName { get; init; }

    #endregion
}
