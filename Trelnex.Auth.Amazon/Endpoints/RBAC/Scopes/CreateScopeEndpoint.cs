using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
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
    /// <param name="resourceNameValidator">The validator for resource names.</param>
    /// <param name="scopeNameValidator">The validator for scope names.</param>
    /// <param name="parameters">The request parameters containing the scope creation details.</param>
    /// <returns>A response confirming the successful creation of the scope.</returns>
    /// <remarks>
    /// This method processes a scope creation request by:
    /// 1. Validating the resource name using the resource name validator
    /// 2. Validating the scope name using the scope name validator
    /// 3. Creating the scope in the RBAC repository if validations pass
    /// 4. Returning a response containing the details of the created scope
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
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IScopeNameValidator scopeNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // Validate the resource name.
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<CreateScopeRequest>();

        // Validate the scope name.
        (var vrScopeName, var scopeName) =
            scopeNameValidator.Validate(
                parameters.Request?.ScopeName);

        vrScopeName.ValidateOrThrow<CreateScopeRequest>();

        // Create the scope.
        await rbacRepository.CreateScopeAsync(
            resourceName: resourceName!,
            scopeName: scopeName!);

        // Return the scope.
        return new CreateScopeResponse
        {
            ResourceName = resourceName!,
            ScopeName = scopeName!
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Encapsulates the parameters for a scope creation request.
    /// </summary>
    /// <remarks>
    /// This class is used as a parameter binding model for the API endpoint,
    /// allowing ASP.NET Core to bind the incoming request body to the request model.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets or initializes the request details for scope creation.
        /// </summary>
        /// <remarks>
        /// This property contains the details required for creating a new scope,
        /// including the resource name and scope name.
        /// </remarks>
        [FromBody]
        public CreateScopeRequest? Request { get; init; }
    }

    #endregion
}
