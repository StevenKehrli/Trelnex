using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for granting a role to a principal for a specific resource.
/// </summary>
/// <remarks>
/// This endpoint allows administrators to assign roles to principals (users or services),
/// thereby granting them specific permissions on resources. It's a core part of the
/// Role-Based Access Control (RBAC) system that establishes the principal-role-resource
/// relationship used for authorization decisions.
/// </remarks>
internal static class GrantPrincipalMembershipEndpoint
{
    #region Private Static Fields

    /// <summary>
    /// Pre-configured validation exception for when the principal ID is missing or invalid.
    /// </summary>
    /// <remarks>
    /// Using a static exception improves performance by avoiding creation of new exception
    /// instances for common validation errors.
    /// </remarks>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GrantPrincipalMembershipRequest).Name}' is not valid.",
        new Dictionary<string, string[]>
        {
            { nameof(GrantPrincipalMembershipRequest.PrincipalId), new[] { "principalId is not valid." } }
        });

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Grant Principal Membership endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC create permission can access this endpoint.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the POST endpoint for granting a principal membership to "/principalmemberships".
        erb.MapPost(
                "/principalmemberships",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<GrantPrincipalMembershipRequest>(MediaTypeNames.Application.Json)
            .Produces<GrantPrincipalMembershipResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GrantPrincipalMembership")
            .WithDescription("Grant a role to a principal.")
            .WithTags("Principal Memberships");
    }

    /// <summary>
    /// Handles requests to the Grant Principal Membership endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="scopeNameValidator">Validates scope name format and compliance.</param>
    /// <param name="roleNameValidator">Validates role name format and compliance.</param>
    /// <param name="parameters">The request parameters containing principal, resource, and role information.</param>
    /// <returns>A response containing the principal's updated memberships after granting the role.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID,
    /// invalid resource name, or invalid role name.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on all inputs before calling the RBAC repository
    /// to grant the role. After successful granting, it returns the principal's complete
    /// set of roles and scopes for the specified resource, which now includes the newly
    /// granted role.
    /// </remarks>
    public static async Task<GrantPrincipalMembershipResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IScopeNameValidator scopeNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // Validate the principal id.
        if (parameters.Request?.PrincipalId is null) throw _validationException;

        // Validate the resource name.
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GrantPrincipalMembershipRequest>();

        // Validate the role name.
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<GrantPrincipalMembershipRequest>();

        // Grant the role to the principal.
        var principalMembership = await rbacRepository.GrantRoleToPrincipalAsync(
            principalId: parameters.Request!.PrincipalId,
            resourceName: resourceName!,
            roleName: roleName!);

        // Return the resource.
        return new GrantPrincipalMembershipResponse
        {
            PrincipalId = principalMembership.PrincipalId,
            ResourceName = principalMembership.ResourceName,
            ScopeNames = principalMembership.ScopeNames,
            RoleNames = principalMembership.RoleNames
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Contains the parameters for the Grant Principal Membership request.
    /// </summary>
    /// <remarks>
    /// This class is used to bind the request body to a strongly-typed object
    /// when the endpoint is invoked.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets the deserialized request body containing principal membership grant parameters.
        /// </summary>
        [FromBody]
        public GrantPrincipalMembershipRequest? Request { get; init; }
    }

    #endregion
}
