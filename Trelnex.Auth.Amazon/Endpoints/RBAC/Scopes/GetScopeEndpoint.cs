using System.Net;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about scopes in the RBAC system.
/// </summary>
/// <remarks>
/// This endpoint allows clients to retrieve details about a specific scope within a resource.
/// Scope information is essential for understanding the authorization boundaries of a protected
/// resource and for managing role assignments that are limited to specific scopes. The endpoint
/// validates both the resource name and scope name before retrieving the scope information.
///
/// This operation is typically used during:
/// - Administration of RBAC models
/// - Auditing of existing authorization boundaries
/// - User interfaces that display or manage scopes
/// - Preparation for creating role assignments within specific scopes
///
/// If the specified scope does not exist, a 404 Not Found response is returned.
/// </remarks>
internal static class GetScopeEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetScopeRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the endpoint for retrieving scope information to the API's routing configuration.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// This method configures a GET endpoint at "/scopes" that accepts a <see cref="GetScopeRequest"/>
    /// in JSON format. The endpoint requires the <see cref="RBACPermission.RBACReadPolicy"/> permission,
    /// ensuring that only authorized users can retrieve scope information. The method defines the possible
    /// response types, including successful retrieval and various error scenarios.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving scope information to "/scopes".
        erb.MapGet(
                "/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Produces<GetScopeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetScope")
            .WithDescription("Gets the specified scope")
            .WithTags("Scopes");
    }

    /// <summary>
    /// Handles the scope retrieval request by validating inputs and fetching the scope information.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="request">The request parameters containing the scope retrieval details.</param>
    /// <returns>A response containing the requested scope information.</returns>
    /// <remarks>
    /// This method processes a scope information request by:
    /// 1. Validating the resource name and scope name from the request
    /// 2. Retrieving the scope from the RBAC repository if validations pass
    /// 3. Transforming the repository data into a standardized API response
    ///
    /// If validation fails, an appropriate exception is thrown, which will be transformed
    /// into an HTTP error response. If the specified scope doesn't exist, a 404 Not Found
    /// error is returned.
    /// </remarks>
    /// <exception cref="ValidationException">
    /// Thrown when the resource name or scope name validation fails.
    /// </exception>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with a 404 Not Found status code when the requested scope doesn't exist.
    /// </exception>
    public static async Task<GetScopeResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [AsParameters] GetScopeRequest request)
    {
        // Validate the request.
        if (request.ResourceName is null) throw _validationException;
        if (request.ScopeName is null) throw _validationException;

        // Get the scope.
        var scope = await rbacRepository.GetScopeAsync(
            resourceName: request.ResourceName,
            scopeName: request.ScopeName)
            ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound); // If the scope is not found, return a 404 Not Found.

        // Return the scope.
        return new GetScopeResponse
        {
            ResourceName = scope.ResourceName,
            ScopeName = scope.ScopeName
        };
    }

    #endregion
}
