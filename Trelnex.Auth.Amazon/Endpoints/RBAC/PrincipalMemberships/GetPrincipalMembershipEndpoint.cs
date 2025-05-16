using System.Net.Mime;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

/// <summary>
/// Provides an endpoint for retrieving information about a principal's role memberships.
/// </summary>
/// <remarks>
/// This endpoint allows clients to query what roles and scopes a principal has been granted
/// for a specific resource. It's primarily used for authorization debugging, auditing, and
/// administrative purposes to understand a user or service's current permissions.
/// </remarks>
internal static class GetPrincipalMembershipEndpoint
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
        $"The '{typeof(GetPrincipalMembershipRequest).Name}' is not valid.",
        new Dictionary<string, string[]>
        {
            { nameof(GetPrincipalMembershipRequest.PrincipalId), new[] { "principalId is not valid." } }
        });

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the Get Principal Membership endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// Configures the endpoint with authentication requirements, request/response content types,
    /// and possible status codes. Only users with the RBAC read permission can access this endpoint.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the GET endpoint for retrieving principal memberships to "/principalmemberships".
        erb.MapGet(
                "/principalmemberships",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetPrincipalMembershipRequest>(MediaTypeNames.Application.Json)
            .Produces<GetPrincipalMembershipResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetPrincipalMembership")
            .WithDescription("Get the role memberships for a principal.")
            .WithTags("Principal Memberships");
    }

    /// <summary>
    /// Handles requests to the Get Principal Membership endpoint.
    /// </summary>
    /// <param name="rbacRepository">The repository for Role-Based Access Control operations.</param>
    /// <param name="resourceNameValidator">Validates resource name format and compliance.</param>
    /// <param name="scopeNameValidator">Validates scope name format and compliance.</param>
    /// <param name="roleNameValidator">Validates role name format and compliance.</param>
    /// <param name="parameters">The request parameters containing principal, resource, and scope information.</param>
    /// <returns>A response containing the principal's memberships for the specified resource and scope.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when the request parameters fail validation, such as missing principal ID,
    /// invalid resource name, or invalid scope name.
    /// </exception>
    /// <remarks>
    /// The endpoint performs validation on all inputs before querying the RBAC repository.
    /// It retrieves the principal's memberships for the specified resource and scope,
    /// and returns them in a standardized response format.
    /// </remarks>
    public static async Task<GetPrincipalMembershipResponse> HandleRequest(
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

        vrResourceName.ValidateOrThrow<GetPrincipalMembershipRequest>();

        // Validate the scope name, if any.
        (var vrScopeName, var scopeName) = parameters.Request?.ScopeName is not null
            ? scopeNameValidator.Validate(
                parameters.Request.ScopeName)
            : (new ValidationResult(), null!); // If no scope name is provided, skip scope name validation.

        vrScopeName.ValidateOrThrow<GetPrincipalMembershipRequest>();

        // Get the role memberships for the principal.
        var principalMembership = await rbacRepository.GetPrincipalMembershipAsync(
            principalId: parameters.Request!.PrincipalId,
            resourceName: resourceName!,
            scopeName: scopeName);

        // Return the resource.
        return new GetPrincipalMembershipResponse
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
    /// Contains the parameters for the Get Principal Membership request.
    /// </summary>
    /// <remarks>
    /// This class is used to bind the request body to a strongly-typed object
    /// when the endpoint is invoked.
    /// </remarks>
    public class RequestParameters
    {
        /// <summary>
        /// Gets the deserialized request body containing principal membership query parameters.
        /// </summary>
        [FromBody]
        public GetPrincipalMembershipRequest? Request { get; init; }
    }

    #endregion
}
