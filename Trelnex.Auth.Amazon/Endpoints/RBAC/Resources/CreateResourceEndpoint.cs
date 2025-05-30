using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for creating a new resource in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to register a new protected resource (such as an API or service)
/// in the Role-Based Access Control system. Resources are the assets that principals can be granted
/// access to through role assignments. Creating a resource is typically the first step in establishing
/// an RBAC model for a given API or service. After a resource is created, roles and scopes can be defined
/// for it, and principals can be granted access through role assignments.
/// </remarks>
internal static class CreateResourceEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(CreateResourceRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Create Resource endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC create permission can access this endpoint.
    /// The endpoint uses HTTP POST semantics to create a new resource entity.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the POST endpoint for creating a resource to "/resources".
        erb.MapPost(
                "/resources",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateResourceRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateResourceResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateResource")
            .WithDescription("Creates a new resource")
            .WithTags("Resources");
    }

    /// <summary>
    /// Handles requests to the Create Resource endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="parameters">The request parameters containing the resource name to create.</param>
    /// <returns>A response confirming the creation of the resource.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name fails validation, such as being too long, containing invalid characters,
    /// or not matching the expected format.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on the resource name before calling the RBAC repository
    /// to create the resource. If a resource with the same name already exists, the repository
    /// may throw an exception which will be translated to an appropriate HTTP error response.
    /// After successful creation, a response containing the resource name is returned to confirm
    /// the operation.
    /// </remarks>
    public static async Task<CreateResourceResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] CreateResourceRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;

        // Create the resource.
        await rbacRepository.CreateResourceAsync(
            resourceName: request.ResourceName);

        // Return the resource.
        return new CreateResourceResponse
        {
            ResourceName = request.ResourceName
        };
    }

    #endregion
}
