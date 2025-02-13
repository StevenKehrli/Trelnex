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

internal static class GetScopeEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                "/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACReadPolicy>()
            .Accepts<GetScopeRequest>(MediaTypeNames.Application.Json)
            .Produces<GetScopeResponse>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status403Forbidden)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status404NotFound)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetsScope")
            .WithDescription("Gets the specified scope")
            .WithTags("Scopes");
    }

    public static async Task<GetScopeResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IScopeNameValidator scopeNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<GetScopeRequest>();

        // validate the scope name
        (var vrScopeName, var scopeName) =
            scopeNameValidator.Validate(
                parameters.Request?.ScopeName);

        vrScopeName.ValidateOrThrow<GetScopeRequest>();

        // get the scope
        var scope = await rbacRepository.GetScopeAsync(
            resourceName: resourceName!,
            scopeName: scopeName!) ?? throw new HttpStatusCodeException(HttpStatusCode.NotFound);

        // return the scope
        return new GetScopeResponse
        {
            ResourceName = scope.ResourceName,
            ScopeName = scope.ScopeName
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public GetScopeRequest? Request { get; init; }
    }
}
