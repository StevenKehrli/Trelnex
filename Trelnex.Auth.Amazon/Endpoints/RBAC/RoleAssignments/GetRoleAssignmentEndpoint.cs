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

internal static class GetRoleAssignmentEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                "/roleassignments",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetRoleAssignmentRequest>(MediaTypeNames.Application.Json)
            .Produces<GetRoleAssignmentResponse>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status404NotFound)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetsRoleAssignment")
            .WithDescription("Gets the specified roleassignment")
            .WithTags("Role Assignments");
    }

    public static async Task<GetRoleAssignmentResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IRoleNameValidator roleNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GetRoleAssignmentRequest>();

        // validate the role name
        (var vrRoleName, var roleName) =
            roleNameValidator.Validate(
                parameters.Request?.RoleName);

        vrRoleName.ValidateOrThrow<GetRoleAssignmentRequest>();

        // get the roleAssignment
        var roleAssignment = await rbacRepository.GetRoleAssignmentAsync(
            resourceName: resourceName!,
            roleName: roleName!) ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound);

        // return the roleassignment
        return new GetRoleAssignmentResponse
        {
            ResourceName = roleAssignment.ResourceName,
            RoleName = roleAssignment.RoleName,
            PrincipalIds = roleAssignment.PrincipalIds
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public GetRoleAssignmentRequest? Request { get; init; }
    }
}
