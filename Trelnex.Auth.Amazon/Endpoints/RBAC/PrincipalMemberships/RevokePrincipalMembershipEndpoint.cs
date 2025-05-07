using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class RevokePrincipalMembershipEndpoint
{
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(RevokePrincipalMembershipRequest).Name}' is not valid.",
        new Dictionary<string, string[]>
        {
            { nameof(RevokePrincipalMembershipRequest.PrincipalId), new[] { "principalId is not valid." } }
        });

    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapDelete(
                "/principalmemberships",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<RevokePrincipalMembershipRequest>(MediaTypeNames.Application.Json)
            .Produces<RevokePrincipalMembershipResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("RevokePrincipalMembership")
            .WithDescription("Revoke a role from a principal.")
            .WithTags("Principal Memberships");
    }

    public static async Task<RevokePrincipalMembershipResponse> HandleRequest(
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

        vrResourceName.ValidateOrThrow<RevokePrincipalMembershipRequest>();

        // validate the role name
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<RevokePrincipalMembershipRequest>();

        // revoke the role from the principal
        var principalMembership = await rbacRepository.RevokeRoleFromPrincipalAsync(
            principalId: parameters.Request!.PrincipalId,
            resourceName: resourceName!,
            roleName: roleName!);

        // return the resource
        return new RevokePrincipalMembershipResponse
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
        public RevokePrincipalMembershipRequest? Request { get; init; }
    }
}
