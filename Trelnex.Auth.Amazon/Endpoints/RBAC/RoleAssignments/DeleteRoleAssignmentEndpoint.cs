using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for deleting a role assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to remove role assignments from principals (users or services),
/// thereby removing specific permissions on resources. It's a core part of the Role-Based
/// Access Control (RBAC) system that helps maintain least-privilege access by removing
/// permissions that are no longer needed.
/// </remarks>
internal static class DeleteRoleAssignmentEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(DeleteRoleAssignmentRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Delete Role Assignment endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC delete permission can access this endpoint.
    /// The endpoint uses HTTP DELETE semantics to indicate the removal of a role assignment.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the DELETE endpoint for deleting a role assignment to "/roleassignments".
        erb.MapDelete(
                "/assignments/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Produces<DeleteRoleAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeleteRoleAssignment")
            .WithDescription("Delete a role assignment for a principal.")
            .WithTags("Role Assignments");
    }

    /// <summary>
    /// Handles requests to the Delete Role Assignment endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="request">The request parameters containing principal, resource, and role information.</param>
    /// <returns>A response containing the principal's updated role assignments after deleting the assignment.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID,
    /// invalid resource name, or invalid role name.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on all inputs before calling the RBAC repository
    /// to delete the role assignment. After successful deletion, it returns the principal's complete
    /// set of remaining roles and scopes for the specified resource, which no longer includes
    /// the deleted role assignment. If the principal had no other roles on the resource, they will
    /// effectively lose access to it.
    /// </remarks>
    public static async Task<DeleteRoleAssignmentResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [AsParameters] DeleteRoleAssignmentRequest request)
    {
        // Validate the request.
        if (request.ResourceName is null) throw _validationException;
        if (request.RoleName is null) throw _validationException;
        if (request.PrincipalId is null) throw _validationException;

        // Delete the role assignment for the principal.
        await rbacRepository.DeleteRoleAssignmentAsync(
            principalId: request.PrincipalId,
            resourceName: request.ResourceName,
            roleName: request.RoleName!);

        // Retrieve the principal's access to the resource after the role assignment deletion.
        var principalAccess = await rbacRepository.GetPrincipalAccessAsync(
            resourceName: request.ResourceName,
            principalId: request.PrincipalId,
            cancellationToken: default);

        // Return the resource.
        return new DeleteRoleAssignmentResponse
        {
            PrincipalId = principalAccess.PrincipalId,
            ResourceName = principalAccess.ResourceName,
            ScopeNames = principalAccess.ScopeNames,
            RoleNames = principalAccess.RoleNames
        };
    }

    #endregion
}
