using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Responses;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class GetRoleEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                "/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetRoleRequest>(MediaTypeNames.Application.Json)
            .Produces<GetRoleResponse>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status404NotFound)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetsRole")
            .WithDescription("Gets the specified role")
            .WithTags("Roles");
    }

    public static async Task<GetRoleResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GetRoleRequest>();

        // validate the role name
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<GetRoleRequest>();

        // get the role
        var role = await rbacRepository.GetRoleAsync(
            resourceName: resourceName!,
            roleName: roleName!) ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound);

        // return the role
        return new GetRoleResponse
        {
            ResourceName = role.ResourceName,
            RoleName = role.RoleName
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public GetRoleRequest? Request { get; init; }
    }
}
