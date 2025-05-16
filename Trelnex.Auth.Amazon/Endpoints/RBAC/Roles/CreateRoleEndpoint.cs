using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for creating new roles for resources in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to define new roles within the context of protected resources.
/// In the RBAC (Role-Based Access Control) system, roles represent collections of permissions that
/// can be assigned to principals (users, services, etc.). Creating a role is a prerequisite before
/// it can be granted to principals, giving them specific access rights to the resource.
///
/// Roles serve as an abstraction layer between principals and resources, allowing administrators
/// to define standardized permission sets that can be easily assigned and managed. This endpoint
/// validates both the resource name and role name before creating the role in the system.
///
/// This operation is typically performed during:
/// - Initial setup of a resource's authorization model
/// - Adding new permission levels to existing resources
/// - Expanding the authorization capabilities of a system
/// </remarks>
internal static class CreateRoleEndpoint
{
    #region Public Static Methods

    /// <summary>
    /// Maps the endpoint for creating a role to the API's routing configuration.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// This method configures a POST endpoint at "/roles" that accepts a <see cref="CreateRoleRequest"/>
    /// in JSON format. The endpoint requires the <see cref="RBACPermission.RBACCreatePolicy"/> permission,
    /// ensuring that only authorized administrators can create roles. The method defines the possible
    /// response types, including successful creation and various error scenarios.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the POST endpoint for creating a role to "/roles".
        erb.MapPost(
                "/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateRoleRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateRoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateRole")
            .WithDescription("Creates a new role")
            .WithTags("Roles");
    }

    /// <summary>
    /// Handles the role creation request by validating inputs and creating the role.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="resourceNameValidator">The validator for resource names.</param>
    /// <param name="roleNameValidator">The validator for role names.</param>
    /// <param name="parameters">The request parameters containing the role creation details.</param>
    /// <returns>A response confirming the successful creation of the role.</returns>
    /// <remarks>
    /// This method processes a role creation request by:
    /// 1. Validating the resource name using the resource name validator
    /// 2. Validating the role name using the role name validator
    /// 3. Creating the role in the RBAC repository if validations pass
    /// 4. Returning a response containing the details of the created role
    ///
    /// The resource must exist in the system before roles can be created for it.
    /// If validation fails, an appropriate exception is thrown, which will be transformed
    /// into an HTTP error response.
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name or role name validation fails.
    /// </exception>
    public static async Task<CreateRoleResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // Validate the resource name.
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<CreateRoleRequest>();

        // Validate the role name.
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<CreateRoleRequest>();

        // Create the role.
        await rbacRepository.CreateRoleAsync(
            resourceName: resourceName!,
            roleName: roleName!);

        // Return the role.
        return new CreateRoleResponse
        {
            ResourceName = resourceName!,
            RoleName = roleName!
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Encapsulates the parameters for a role creation request.
    /// </summary>
    /// <remarks>
    /// This class is used as a parameter binding model for the API endpoint,
    /// allowing ASP.NET Core to bind the incoming request body to the request model.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets or initializes the request details for role creation.
        /// </summary>
        /// <remarks>
        /// This property contains the details required for creating a new role,
        /// including the resource name and role name.
        /// </remarks>
        [FromBody]
        public CreateRoleRequest? Request { get; init; }
    }

    #endregion
}
