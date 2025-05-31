using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for deleting a resource and all its associated data from the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to completely remove a resource (such as an API or service) and all of its
/// associated roles, scopes, and role assignments from the Role-Based Access Control system.
/// This operation is typically performed when a resource is being decommissioned or is no longer
/// needed for authorization purposes. The operation cascades to remove all roles, scopes, and
/// principal role assignments associated with this resource.
/// </remarks>
internal static class DeleteResourceEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(DeleteResourceRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Delete Resource endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC delete permission can access this endpoint.
    /// The endpoint uses HTTP DELETE semantics to indicate the removal of a resource entity.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the DELETE endpoint for deleting a resource to "/resources".
        erb.MapDelete(
                "/resources",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeleteResourceRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeleteResource")
            .WithDescription("Deletes the specified resource")
            .WithTags("Resources");
    }

    /// <summary>
    /// Handles requests to the Delete Resource endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="parameters">The request parameters containing the resource name to delete.</param>
    /// <returns>An HTTP 200 OK result if the operation succeeds.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name fails validation, such as being too long, containing invalid characters,
    /// or not matching the expected format.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on the resource name before calling the RBAC repository
    /// to delete the resource. This operation is cascading and will remove all roles, scopes, and
    /// principal role assignments associated with this resource. After successful deletion, a 200 OK
    /// response is returned with no content.
    ///
    /// If the resource does not exist, the repository may throw an exception which will be translated
    /// to a 404 Not Found HTTP error response.
    /// </remarks>
    public static async Task<IResult> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] DeleteResourceRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;

        // Delete the resource.
        await rbacRepository.DeleteResourceAsync(
            resourceName: request.ResourceName);

        // Return an Ok result.
        return Results.Ok();
    }

    #endregion
}
