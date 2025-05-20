using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Handles authorization based on permission policies defined using <see cref="IPermissionPolicy"/>.
/// </summary>
/// <remarks>
/// This handler evaluates security requirements for the specified permission policy,
/// checking both scope claims and roles to determine access authorization.
/// The handler grants authorization if the user meets all specified security requirements.
/// </remarks>
internal class PermissionRequirementAuthorizationHandler(
    ISecurityProvider securityProvider)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <summary>
    /// Handles the authorization requirement by evaluating the associated permission policy.
    /// </summary>
    /// <param name="context">The authorization handler context containing the user's claims.</param>
    /// <param name="requirement">The permission requirement containing the policy to evaluate.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return Task.CompletedTask;
        }

        // Retrieve the user context from the HTTP context, or evaluate requirements if it doesn't exist.
        var userContext = httpContext.GetUserContext()
            ?? EvaluateRequirements(
                user: context.User,
                endpoint: endpoint);

        // Store the user context in the HTTP context.
        httpContext.SetUserContext(userContext);

        // Succeed if the user is authorized.
        if (userContext.IsAuthorized)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Evaluates the security requirements for the endpoint and creates a <see cref="UserContext"/>.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <param name="endpoint">The endpoint to evaluate requirements for.</param>
    /// <returns>A <see cref="UserContext"/> based on the evaluated requirements.</returns>
    private UserContext EvaluateRequirements(
        ClaimsPrincipal user,
        Endpoint endpoint)
    {
        var authorizedPolicies = endpoint.Metadata
            .GetOrderedMetadata<PermissionAttribute>()
            .Where(pa =>
            {
                var securityRequirement = securityProvider.GetSecurityRequirement(pa.Policy!);

                // Check if the user has the required scope claim
                var hasScope = user.HasClaim(ClaimConstants.Scope, securityRequirement.Scope);

                // Check if the user has at least one of the required roles
                var hasRole = securityRequirement.RequiredRoles.Any(user.IsInRole);

                // User must have both the required scope AND at least one required role
                return hasScope & hasRole;
            })
            .Select(pa => pa.Policy!)
            .ToArray();

        return new UserContext(user, authorizedPolicies);
    }
}
