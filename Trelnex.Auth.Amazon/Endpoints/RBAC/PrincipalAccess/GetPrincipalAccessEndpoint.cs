using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about a principal's access permissions.
/// </summary>
/// <remarks>
/// This endpoint allows clients to query what roles and scopes a principal has been granted
/// for a specific resource. It's primarily used for authorization debugging, auditing, and
/// administrative purposes to understand a user or service's current permissions.
/// </remarks>
internal static class GetPrincipalAccessEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetPrincipalAccessRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Get Principal Access endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC read permission can access this endpoint.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving principal access to "/principals".
        erb.MapGet(
                "/assignments/principals",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Produces<GetPrincipalAccessResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetPrincipalAccess")
            .WithDescription("Get the access permissions for a principal.")
            .WithTags("Principal Access");
    }

    /// <summary>
    /// Handles requests to the Get Principal Access endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="request">The request parameters containing principal, resource, and scope information.</param>
    /// <returns>A response containing the principal's access permissions for the specified resource and scope.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID,
    /// invalid resource name, or invalid scope name.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on all inputs before querying the RBAC repository.
    /// It retrieves the principal's access permissions for the specified resource and scope,
    /// and returns them in a standardized response format.
    /// </remarks>
    public static async Task<GetPrincipalAccessResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [AsParameters] GetPrincipalAccessRequest request)
    {
        // Validate the request.
        if (request.ResourceName is null) throw _validationException;
        if (request.PrincipalId is null) throw _validationException;

        // Get the access permissions for the principal.
        var principalAccess = await rbacRepository.GetPrincipalAccessAsync(
            resourceName: request.ResourceName,
            principalId: request.PrincipalId);

        // Return the resource.
        return new GetPrincipalAccessResponse
        {
            PrincipalId = principalAccess.PrincipalId,
            ResourceName = principalAccess.ResourceName,
            ScopeNames = principalAccess.ScopeNames,
            RoleNames = principalAccess.RoleNames
        };
    }

    #endregion
}
