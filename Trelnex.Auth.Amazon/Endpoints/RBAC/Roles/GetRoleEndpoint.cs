using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about roles in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows clients to retrieve details about a specific role within a resource.
/// Role information is essential for understanding the authorization model of a protected resource
/// and for managing role assignments to principals. The endpoint validates both the resource name
/// and role name before retrieving the role information.
///
/// This operation is typically used during:
/// - Administration of RBAC models
/// - Auditing of existing permissions
/// - User interfaces that display or manage roles
/// - Preparation for granting roles to principals
///
/// If the specified role does not exist, a 404 Not Found response is returned.
/// </remarks>
internal static class GetRoleEndpoint
{
    #region Public Static Methods

    /// <summary>
    /// Maps the endpoint for retrieving role information to the API's routing configuration.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// This method configures a GET endpoint at "/roles" that accepts a <see cref="GetRoleRequest"/>
    /// in JSON format. The endpoint requires the <see cref="RBACPermission.RBACReadPolicy"/> permission,
    /// ensuring that only authorized users can retrieve role information. The method defines the possible
    /// response types, including successful retrieval and various error scenarios.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving role information to "/roles".
        erb.MapGet(
                "/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetRoleRequest>(MediaTypeNames.Application.Json)
            .Produces<GetRoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetsRole")
            .WithDescription("Gets the specified role")
            .WithTags("Roles");
    }

    /// <summary>
    /// Handles the role retrieval request by validating inputs and fetching the role information.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="resourceNameValidator">The validator for resource names.</param>
    /// <param name="roleNameValidator">The validator for role names.</param>
    /// <param name="parameters">The request parameters containing the role retrieval details.</param>
    /// <returns>A response containing the requested role information.</returns>
    /// <remarks>
    /// This method processes a role information request by:
    /// 1. Validating the resource name using the resource name validator
    /// 2. Validating the role name using the role name validator
    /// 3. Retrieving the role from the RBAC repository if validations pass
    /// 4. Transforming the repository data into a standardized API response
    ///
    /// If validation fails, an appropriate exception is thrown, which will be transformed
    /// into an HTTP error response. If the specified role doesn't exist, a 404 Not Found
    /// error is returned.
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name or role name validation fails.
    /// </exception>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with a 404 Not Found status code when the requested role doesn't exist.
    /// </exception>
    public static async Task<GetRoleResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // Validate the resource name.
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GetRoleRequest>();

        // Validate the role name.
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<GetRoleRequest>();

        // Get the role.
        var role = await rbacRepository.GetRoleAsync(
            resourceName: resourceName!,
            roleName: roleName!) ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound); // If the role is not found, throw a 404 Not Found exception.

        // Return the role.
        return new GetRoleResponse
        {
            ResourceName = role.ResourceName,
            RoleName = role.RoleName
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Encapsulates the parameters for a role information retrieval request.
    /// </summary>
    /// <remarks>
    /// This class is used as a parameter binding model for the API endpoint,
    /// allowing ASP.NET Core to bind the incoming request body to the request model.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets or initializes the request details for role information retrieval.
        /// </summary>
        /// <remarks>
        /// This property contains the details required for identifying the role to retrieve,
        /// including the resource name and role name.
        /// </remarks>
        [FromBody]
        public GetRoleRequest? Request { get; init; }
    }

    #endregion
}
