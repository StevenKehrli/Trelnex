using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class GrantPrincipalMembershipEndpoint
{
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GrantPrincipalMembershipRequest).Name}' is not valid.",
        new Dictionary<string, string[]>
        {
            { nameof(GrantPrincipalMembershipRequest.PrincipalId), new[] { "principalId is not valid." } }
        });

    public static void Map(
        IEndpointRouteBuilder erb)
    {
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

    public static async Task<GrantPrincipalMembershipResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IScopeNameValidator scopeNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the principal id
        if (parameters.Request?.PrincipalId is null) throw _validationException;

        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GrantPrincipalMembershipRequest>();

        // validate the role name
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<GrantPrincipalMembershipRequest>();

        // grant the role to the principal
        var principalMembership = await rbacRepository.GrantRoleToPrincipalAsync(
            principalId: parameters.Request!.PrincipalId,
            resourceName: resourceName!,
            roleName: roleName!);

        // return the resource
        return new GrantPrincipalMembershipResponse
        {
            PrincipalId = principalMembership.PrincipalId,
            ResourceName = principalMembership.ResourceName,
            ScopeNames = principalMembership.ScopeNames,
            RoleNames = principalMembership.RoleNames
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public GrantPrincipalMembershipRequest? Request { get; init; }
    }
}
