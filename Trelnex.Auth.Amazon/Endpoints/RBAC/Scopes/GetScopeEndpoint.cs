using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
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
            .Accepts<GetScopeRequest>(MediaTypeNames.Application.Json)
            .Produces<GetScopeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetsScope")
            .WithDescription("Gets the specified scope")
            .WithTags("Scopes");
    }

    /// <summary>
    /// Handles the scope retrieval request by validating inputs and fetching the scope information.
    /// </summary>
    /// <param name="rbacRepository">The repository for RBAC operations.</param>
    /// <param name="resourceNameValidator">The validator for resource names.</param>
    /// <param name="scopeNameValidator">The validator for scope names.</param>
    /// <param name="parameters">The request parameters containing the scope retrieval details.</param>
    /// <returns>A response containing the requested scope information.</returns>
    /// <remarks>
    /// This method processes a scope information request by:
    /// 1. Validating the resource name using the resource name validator
    /// 2. Validating the scope name using the scope name validator
    /// 3. Retrieving the scope from the RBAC repository if validations pass
    /// 4. Transforming the repository data into a standardized API response
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
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IScopeNameValidator scopeNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // Validate the resource name.
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GetScopeRequest>();

        // Validate the scope name.
        (var vrScopeName, var scopeName) =
            scopeNameValidator.Validate(
                parameters.Request?.ScopeName);

        vrScopeName.ValidateOrThrow<GetScopeRequest>();

        // Get the scope.
        var scope = await rbacRepository.GetScopeAsync(
            resourceName: resourceName!,
            scopeName: scopeName!) ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound); // If the scope is not found, return a 404 Not Found.

        // Return the scope.
        return new GetScopeResponse
        {
            ResourceName = scope.ResourceName,
            ScopeName = scope.ScopeName
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Encapsulates the parameters for a scope information retrieval request.
    /// </summary>
    /// <remarks>
    /// This class is used as a parameter binding model for the API endpoint,
    /// allowing ASP.NET Core to bind the incoming request body to the request model.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets or initializes the request details for scope information retrieval.
        /// </summary>
        /// <remarks>
        /// This property contains the details required for identifying the scope to retrieve,
        /// including the resource name and scope name.
        /// </remarks>
        [FromBody]
        public GetScopeRequest? Request { get; init; }
    }

    #endregion
}
