using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about all principals assigned to a specific role for a resource.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to query which principals (users or services) have been granted
/// a specific role for a particular resource in the RBAC system. This information is useful for
/// administrative purposes, auditing, and understanding the current authorization state.
/// It enables administrators to see all entities that have a certain level of access to a resource,
/// which is valuable for security reviews and compliance purposes.
/// </remarks>
internal static class GetRoleAssignmentEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetRoleAssignmentRequest).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Get Role Assignment endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder to configure the endpoint on.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC read permission can access this endpoint.
    /// The endpoint uses HTTP GET semantics to retrieve role assignment information.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving role assignment information to "/roleassignments".
        erb.MapGet(
                "/assignments/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetRoleAssignmentRequest>(MediaTypeNames.Application.Json)
            .Produces<GetRoleAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetRoleAssignment")
            .WithDescription("Gets the specified role assignment")
            .WithTags("Role Assignments");
    }

    /// <summary>
    /// Handles requests to the Get Role Assignment endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="roleNameValidator">Validates role name format and compliance.</param>
    /// <param name="parameters">The request parameters containing resource and role information.</param>
    /// <returns>A response containing information about all principals assigned to the specified role for the resource.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as invalid resource name or role name.
    /// </exception>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown with a 404 Not Found status code when the requested resource-role combination does not exist.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on the resource name and role name before querying the RBAC repository
    /// for the role assignment information. If the resource-role combination exists, the endpoint returns
    /// a response containing the resource name, role name, and an array of principal IDs that have been
    /// assigned this role for the resource. If the resource-role combination does not exist, a 404 Not Found
    /// response is returned.
    /// </remarks>
    public static async Task<GetRoleAssignmentResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromBody] GetRoleAssignmentRequest? request)
    {
        // Validate the request.
        if (request is null) throw _validationException;
        if (request.ResourceName is null) throw _validationException;
        if (request.RoleName is null) throw _validationException;

        // Retrieve all principals assigned to the specified role for the resource.
        var principalIds = await rbacRepository.GetPrincipalsForRoleAsync(
            resourceName: request.ResourceName,
            roleName: request.RoleName!);

        // Return the role assignment information with all assigned principals.
        return new GetRoleAssignmentResponse
        {
            ResourceName = request.ResourceName,
            RoleName = request.RoleName!,
            PrincipalIds = principalIds
        };
    }

    #endregion
}
