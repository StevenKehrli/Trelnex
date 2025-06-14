using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for creating a scope assignment for a principal on a specific resource.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to create scope assignments for principals (users or services),
/// thereby assigning them specific scope boundaries on resources. It's a core part of the
/// Role-Based Access Control (RBAC) system that establishes the principal-scope-resource
/// relationship used for authorization decisions.
/// </remarks>
internal static class CreateScopeAssignmentEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(CreateScopeAssignmentRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Create Scope Assignment endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC create permission can access this endpoint.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the POST endpoint for creating a scope assignment to "/scopeassignments".
        erb.MapPost(
                "/assignments/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateScopeAssignmentRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateScopeAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateScopeAssignment")
            .WithDescription("Create a scope assignment for a principal.")
            .WithTags("Scope Assignments");
    }

    /// <summary>
    /// Handles requests to the Create Scope Assignment endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="request">The request body containing principal, resource, and scope information.</param>
    /// <returns>A response containing the principal's updated scope assignments after creating the assignment.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID,
    /// invalid resource name, or invalid scope name.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on all inputs before calling the RBAC repository
    /// to create the scope assignment. After successful creation, it returns the principal's complete
    /// set of roles and scopes for the specified resource, which now includes the newly
    /// created scope assignment.
    /// </remarks>
    public static async Task<CreateScopeAssignmentResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] CreateScopeAssignmentRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;
        if (request.ScopeName is null) throw _validationException;
        if (request.PrincipalId is null) throw _validationException;

        // Create the scope assignment for the principal.
        await rbacRepository.CreateScopeAssignmentAsync(
            resourceName: request.ResourceName,
            scopeName: request.ScopeName,
            principalId: request.PrincipalId,
            cancellationToken: default);

        // Retrieve the principal's access to the resource after the scope assignment.
        var principalAccess = await rbacRepository.GetPrincipalAccessAsync(
            principalId: request.PrincipalId,
            resourceName: request.ResourceName,
            cancellationToken: default);

        // Return the resource.
        return new CreateScopeAssignmentResponse
        {
            PrincipalId = principalAccess.PrincipalId,
            ResourceName = principalAccess.ResourceName,
            ScopeNames = principalAccess.ScopeNames,
            RoleNames = principalAccess.RoleNames
        };
    }

    #endregion
}
