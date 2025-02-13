using System.Net.Mime;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Responses;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class GetPrincipalMembershipEndpoint
{
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetPrincipalMembershipRequest).Name}' is not valid.",
        new Dictionary<string, string[]>
        {
            { nameof(GetPrincipalMembershipRequest.PrincipalId), new[] { "principalId is not valid." } }
        });

    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                "/principalmemberships",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetPrincipalMembershipRequest>(MediaTypeNames.Application.Json)
            .Produces<GetPrincipalMembershipResponse>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetPrincipalMembership")
            .WithDescription("Get the role memberships for a principal.")
            .WithTags("Principal Memberships");
    }

    public static async Task<GetPrincipalMembershipResponse> HandleRequest(
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

        vrResourceName.ValidateOrThrow<GetPrincipalMembershipRequest>();

        // validate the scope name, if any
        (var vrScopeName, var scopeName) = parameters.Request?.ScopeName is not null
            ? scopeNameValidator.Validate(
                parameters.Request.ScopeName)
            : (new ValidationResult(), null!);

        vrScopeName.ValidateOrThrow<GetPrincipalMembershipRequest>();

        // get the role memberships for the principal
        var principalMembership = await rbacRepository.GetPrincipalMembershipAsync(
            principalId: parameters.Request!.PrincipalId,
            resourceName: resourceName!,
            scopeName: scopeName);

        // return the resource
        return new GetPrincipalMembershipResponse
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
        public GetPrincipalMembershipRequest? Request { get; init; }
    }
}
