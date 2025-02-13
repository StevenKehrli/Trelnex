using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Responses;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class CreateRoleEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapPost(
                "/roles",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateRoleRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateRoleResponse>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateRole")
            .WithDescription("Creates a new role")
            .WithTags("Roles");
    }

    public static async Task<CreateRoleResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<CreateRoleRequest>();

        // validate the role name
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<CreateRoleRequest>();

        // create the role
        await rbacRepository.CreateRoleAsync(
            resourceName: resourceName!,
            roleName: roleName!);

        // return the role
        return new CreateRoleResponse
        {
            ResourceName = resourceName!,
            RoleName = roleName!
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public CreateRoleRequest? Request { get; init; }
    }
}
