using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about all principals assigned to a specific scope for a resource.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to query which principals (users or services) have been granted
/// a specific scope for a particular resource in the RBAC system. This information is useful for
/// administrative purposes, auditing, and understanding the current authorization state.
/// It enables administrators to see all entities that have a certain scope boundary for a resource,
/// which is valuable for security reviews and compliance purposes.
/// </remarks>
internal static class GetScopeAssignmentEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetScopeAssignmentRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Get Scope Assignment endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC read permission can access this endpoint.
    /// The endpoint uses HTTP GET semantics to retrieve scope assignment information.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving scope assignment information to "/scopeassignments".
        erb.MapGet(
                "/assignments/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetScopeAssignmentRequest>(MediaTypeNames.Application.Json)
            .Produces<GetScopeAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetScopeAssignment")
            .WithDescription("Gets the specified scope assignment")
            .WithTags("Scope Assignments");
    }

    /// <summary>
    /// Handles requests to the Get Scope Assignment endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="scopeNameValidator">Validates scope name format and compliance.</param>
    /// <param name="parameters">The request parameters containing resource and scope information.</param>
    /// <returns>A response containing information about all principals assigned to the specified scope for the resource.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as invalid resource name or scope name.
    /// </exception>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with a 404 Not Found status code when the requested resource-scope combination does not exist.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on the resource name and scope name before querying the RBAC repository
    /// for the scope assignment information. If the resource-scope combination exists, the endpoint returns
    /// a response containing the resource name, scope name, and an array of principal IDs that have been
    /// assigned this scope for the resource. If the resource-scope combination does not exist, a 404 Not Found
    /// response is returned.
    /// </remarks>
    public static async Task<GetScopeAssignmentResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] GetScopeAssignmentRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;
        if (request.ScopeName is null) throw _validationException;

        // Retrieve all principals assigned to the specified scope for the resource.
        var principalIds = await rbacRepository.GetPrincipalsForScopeAsync(
            resourceName: request.ResourceName,
            scopeName: request.ScopeName);

        // Return the scope assignment information with all assigned principals.
        return new GetScopeAssignmentResponse
        {
            ResourceName = request.ResourceName,
            ScopeName = request.ScopeName,
            PrincipalIds = principalIds
        };
    }

    #endregion
}
