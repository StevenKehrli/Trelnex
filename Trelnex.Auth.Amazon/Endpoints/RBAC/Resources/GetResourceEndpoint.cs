using System.Net;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about a specific resource in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows clients to obtain detailed information about a resource that has been
/// registered in the Role-Based Access Control system. The response includes the resource name
/// and all roles and scopes defined for the resource. This information is useful for administrative
/// purposes, auditing, and understanding the authorization model for a specific resource.
/// </remarks>
internal static class GetResourceEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetResourceRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Get Resource endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC read permission can access this endpoint.
    /// The endpoint uses HTTP GET semantics to retrieve a resource entity.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving resource information to "/resources".
        erb.MapGet(
                "/resources",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Produces<GetResourceResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetResource")
            .WithDescription("Gets the specified resource")
            .WithTags("Resources");
    }

    /// <summary>
    /// Handles requests to the Get Resource endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="request">The request parameters containing the resource name to retrieve.</param>
    /// <returns>A response containing detailed information about the requested resource.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name fails validation, such as being too long, containing invalid characters,
    /// or not matching the expected format.
    /// </exception>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with a 404 Not Found status code when the requested resource does not exist.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on the resource name before querying the RBAC repository
    /// for the resource. If the resource exists, the endpoint returns a response containing the
    /// resource name, all scopes defined for the resource, and all roles defined for the resource.
    /// If the resource does not exist, a 404 Not Found response is returned.
    /// </remarks>
    public static async Task<GetResourceResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [AsParameters] GetResourceRequest request)
    {
        // Validate the request.
        if (request.ResourceName is null) throw _validationException;

        // Get the resource.
        var resource = await rbacRepository.GetResourceAsync(
            resourceName: request.ResourceName);

        if (resource is null)
        {
            throw new HttpStatusCodeException(
                HttpStatusCode.NotFound,
                $"Resource '{request.ResourceName}' not found.");
        }

        // Return the resource.
        return new GetResourceResponse
        {
            ResourceName = resource.ResourceName,
            ScopeNames = resource.ScopeNames,
            RoleNames = resource.RoleNames
        };
    }

    #endregion
}
