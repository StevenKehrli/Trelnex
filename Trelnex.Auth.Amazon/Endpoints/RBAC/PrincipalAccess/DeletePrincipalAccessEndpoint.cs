using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for deleting a principal and all its role assignments from the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to completely remove a principal (user or service)
/// from the RBAC system. The operation cascades to remove all role assignments for this principal
/// across all resources and scopes, effectively revoking all access permissions. This is typically
/// performed when a principal no longer needs any access or when it has been deprovisioned
/// from the identity system.
/// </remarks>
internal static class DeletePrincipalAccessEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(DeletePrincipalAccessRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Delete Principal endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC delete permission can access this endpoint.
    /// The endpoint uses HTTP DELETE semantics to indicate the removal of a principal entity.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the DELETE endpoint for deleting a principal to "/assignments/principals".
        erb.MapDelete(
                "assignments/principals",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeletePrincipalAccess")
            .WithDescription("Deletes the access permissions for the specified principal")
            .WithTags("Principal Access");
    }

    /// <summary>
    /// Handles requests to the Delete Principal endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="request">The request parameters containing the principal ID to delete.</param>
    /// <returns>An HTTP 200 OK result if the operation succeeds.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on the principal ID before calling the RBAC repository
    /// to delete the principal. This operation is cascading and will remove all role assignments
    /// for the principal across all resources and scopes. After successful deletion, a 200 OK
    /// response is returned with no content.
    /// </remarks>
    public static async Task<IResult> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [AsParameters] DeletePrincipalAccessRequest request)
    {
        // Validate the request.
        if (request.PrincipalId is null) throw _validationException;

        // Delete the principal.
        await rbacRepository.DeletePrincipalAsync(
            principalId: request.PrincipalId);

        // Return an Ok result.
        return Results.Ok();
    }

    #endregion
}
