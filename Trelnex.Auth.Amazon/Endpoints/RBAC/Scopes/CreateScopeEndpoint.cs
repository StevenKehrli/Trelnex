using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for creating new scopes for resources in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to define new authorization boundaries (scopes) within
/// the context of protected resources. In the RBAC (Role-Based Access Control) system, scopes
/// represent authorization boundaries that limit the context in which resources can be accessed.
/// Common examples include environments (dev, test, prod), geographical regions, or logical domains.
///
/// Creating a scope is typically done during the initial setup of a resource's authorization model
/// to segregate access across different environments or contexts. This endpoint validates both the
/// resource name and scope name before creating the scope in the system.
///
/// This operation is typically performed during:
/// - Initial setup of a resource's authorization model
/// - Adding new environments or regions to existing resources
/// - Expanding the authorization boundaries of a system
/// </remarks>
internal static class CreateScopeEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(CreateScopeRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the endpoint for creating a scope to the API's routing configuration.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// This method configures a POST endpoint at "/scopes" that accepts a <see cref="CreateScopeRequest"/>
    /// in JSON format. The endpoint requires the <see cref="RBACPermission.RBACCreatePolicy"/> permission,
    /// ensuring that only authorized administrators can create scopes. The method defines the possible
    /// response types, including successful creation and various error scenarios.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the POST endpoint for creating a scope to "/scopes".
        erb.MapPost(
                "/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateScopeRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateScopeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateScope")
            .WithDescription("Creates a new scope")
            .WithTags("Scopes");
    }

    /// <summary>
    /// Handles the scope creation request by validating inputs and creating the scope.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="request">The request body containing the scope creation details.</param>
    /// <returns>A response confirming the successful creation of the scope.</returns>
    /// <remarks>
    /// This method processes a scope creation request by:
    /// 1. Validating the resource name and scope name from the request
    /// 2. Creating the scope in the RBAC repository if validations pass
    /// 3. Returning a response containing the details of the created scope
    ///
    /// The resource must exist in the system before scopes can be created for it.
    /// If validation fails, an appropriate exception is thrown, which will be transformed
    /// into an HTTP error response.
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name or scope name validation fails.
    /// </exception>
    public static async Task<CreateScopeResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] CreateScopeRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;
        if (request.ScopeName is null) throw _validationException;

        // Create the scope.
        await rbacRepository.CreateScopeAsync(
            resourceName: request.ResourceName,
            scopeName: request.ScopeName);

        // Return the scope.
        return new CreateScopeResponse
        {
            ResourceName = request.ResourceName,
            ScopeName = request.ScopeName
        };
    }

    #endregion
}
