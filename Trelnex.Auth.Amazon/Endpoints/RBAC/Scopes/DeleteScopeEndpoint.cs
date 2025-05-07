using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class DeleteScopeEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapDelete(
                "/scopes",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeleteScopeRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeletesScope")
            .WithDescription("Deletes the specified scope")
            .WithTags("Scopes");
    }

    public static async Task<IResult> HandleRequest(
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

        vrScopeName.ValidateOrThrow<DeleteScopeRequest>();

        // delete the scope
        await rbacRepository.DeleteScopeAsync(
            resourceName: resourceName!,
            scopeName: scopeName!);

        return Results.Ok();
    }

    public class RequestParameters
    {
        [FromBody]
        public DeleteScopeRequest? Request { get; init; }
    }
}
