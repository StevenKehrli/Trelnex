using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for deleting roles from resources in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to remove a role from a protected resource.
/// When a role is deleted, all principal memberships (role assignments) associated with
/// this role are automatically revoked, effectively removing these permissions from
/// all principals who had been granted this role. The endpoint validates both the
/// resource name and role name before proceeding with the deletion operation.
///
/// This operation is typically used during:
/// - Cleanup of deprecated authorization models
/// - Removal of access levels that are no longer needed
/// - Restructuring the authorization model for a resource
///
/// Note that role deletion is a significant operation that affects all principals
/// with this role assignment and cannot be undone. Deleted roles must be recreated
/// if needed again in the future.
/// </remarks>
internal static class DeleteRoleEndpoint
{
    #region Public Static Methods

    /// <summary>
    /// Maps the endpoint for deleting a role to the API's routing configuration.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// This method configures a DELETE endpoint at "/roles" that accepts a <see cref="DeleteRoleRequest"/>
    /// in JSON format. The endpoint requires the <see cref="RBACPermission.RBACDeletePolicy"/> permission,
    /// ensuring that only authorized administrators can delete roles. The method defines the possible
    /// response types, including successful completion and various error scenarios.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the DELETE endpoint for deleting a role to "/roles".
        erb.MapDelete(
                "/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeleteRoleRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeletesRole")
            .WithDescription("Deletes the specified role")
            .WithTags("Roles");
    }

    /// <summary>
    /// Handles the role deletion request by validating inputs and performing the deletion operation.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="resourceNameValidator">The validator for resource names.</param>
    /// <param name="roleNameValidator">The validator for role names.</param>
    /// <param name="parameters">The request parameters containing the role deletion details.</param>
    /// <returns>An HTTP result indicating the outcome of the operation.</returns>
    /// <remarks>
    /// This method processes a role deletion request by:
    /// 1. Validating the resource name using the resource name validator
    /// 2. Validating the role name using the role name validator
    /// 3. Deleting the role from the RBAC repository if validations pass
    ///
    /// If validation fails, an appropriate exception is thrown, which will be transformed
    /// into an appropriate HTTP error response.
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name or role name validation fails.
    /// </exception>
    public static async Task<IResult> HandleRequest(
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

        vrRoleName.ValidateOrThrow<DeleteRoleRequest>();

        // Delete the role.
        await rbacRepository.DeleteRoleAsync(
            resourceName: resourceName!,
            roleName: roleName!);

        // Return an Ok result.
        return Results.Ok();
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Encapsulates the parameters for a role deletion request.
    /// </summary>
    /// <remarks>
    /// This class is used as a parameter binding model for the API endpoint,
    /// allowing ASP.NET Core to bind the incoming request body to the request model.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets or initializes the request details for role deletion.
        /// </summary>
        /// <remarks>
        /// This property contains the details required for identifying the role to delete,
        /// including the resource name and role name.
        /// </remarks>
        [FromBody]
        public DeleteRoleRequest? Request { get; init; }
    }

    #endregion
}
