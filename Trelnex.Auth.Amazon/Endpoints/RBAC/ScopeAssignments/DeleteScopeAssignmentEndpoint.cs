using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for deleting a scope assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to remove scope assignments from principals (users or services),
/// thereby removing specific scope boundaries on resources. It's a core part of the Role-Based
/// Access Control (RBAC) system that helps maintain least-privilege access by removing
/// scope boundaries that are no longer needed.
/// </remarks>
internal static class DeleteScopeAssignmentEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(DeleteScopeAssignmentRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Delete Scope Assignment endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC delete permission can access this endpoint.
    /// The endpoint uses HTTP DELETE semantics to indicate the removal of a scope assignment.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the DELETE endpoint for deleting a scope assignment to "/scopeassignments".
        erb.MapDelete(
                "/assignments/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeleteScopeAssignmentRequest>(MediaTypeNames.Application.Json)
            .Produces<DeleteScopeAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeleteScopeAssignment")
            .WithDescription("Delete a scope assignment for a principal.")
            .WithTags("Scope Assignments");
    }

    /// <summary>
    /// Handles requests to the Delete Scope Assignment endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="scopeNameValidator">Validates scope name format and compliance.</param>
    /// <param name="roleNameValidator">Validates role name format and compliance.</param>
    /// <param name="parameters">The request parameters containing principal, resource, and scope information.</param>
    /// <returns>A response containing the principal's updated scope assignments after deleting the assignment.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID,
    /// invalid resource name, or invalid scope name.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on all inputs before calling the RBAC repository
    /// to delete the scope assignment. After successful deletion, it returns the principal's complete
    /// set of remaining roles and scopes for the specified resource, which no longer includes
    /// the deleted scope assignment.
    /// </remarks>
    public static async Task<DeleteScopeAssignmentResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] DeleteScopeAssignmentRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;
        if (request.ScopeName is null) throw _validationException;
        if (request.PrincipalId is null) throw _validationException;

        // Delete the scope assignment for the principal.
        await rbacRepository.DeleteScopeAssignmentAsync(
            resourceName: request.ResourceName,
            scopeName: request.ScopeName,
            principalId: request.PrincipalId);

        // Retrieve the principal's access to the resource after the scope assignment deletion.
        var principalAccess = await rbacRepository.GetPrincipalAccessAsync(
            principalId: request.PrincipalId,
            resourceName: request.ResourceName,
            cancellationToken: default);

        // Return the resource.
        return new DeleteScopeAssignmentResponse
        {
            PrincipalId = principalAccess.PrincipalId,
            ResourceName = principalAccess.ResourceName,
            ScopeNames = principalAccess.ScopeNames,
            RoleNames = principalAccess.RoleNames
        };
    }

    #endregion
}
