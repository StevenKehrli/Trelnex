using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class CreateScopeEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapPost(
                "/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateScopeRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateScopeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateScope")
            .WithDescription("Creates a new scope")
            .WithTags("Scopes");
    }

    public static async Task<CreateScopeResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [FromServices] IScopeNameValidator scopeNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<CreateScopeRequest>();

        // validate the scope name
        (var vrScopeName, var scopeName) =
            scopeNameValidator.Validate(
                parameters.Request?.ScopeName);

        vrScopeName.ValidateOrThrow<CreateScopeRequest>();

        // create the scope
        await rbacRepository.CreateScopeAsync(
            resourceName: resourceName!,
            scopeName: scopeName!);

        // return the scope
        return new CreateScopeResponse
        {
            ResourceName = resourceName!,
            ScopeName = scopeName!
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public CreateScopeRequest? Request { get; init; }
    }
}
