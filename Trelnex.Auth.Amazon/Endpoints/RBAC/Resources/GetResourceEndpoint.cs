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

internal static class GetResourceEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                "/resources",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetResourceRequest>(MediaTypeNames.Application.Json)
            .Produces<GetResourceResponse>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status404NotFound)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetsResource")
            .WithDescription("Gets the specified resource")
            .WithTags("Resources");
    }

    public static async Task<GetResourceResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GetResourceRequest>();

        // get the resource
        var resource = await rbacRepository.GetResourceAsync(
            resourceName: resourceName!) ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound);

        // return the resource
        return new GetResourceResponse
        {
            ResourceName = resource.ResourceName,
            ScopeNames = resource.ScopeNames,
            RoleNames = resource.RoleNames
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public GetResourceRequest? Request { get; init; }
    }
}
