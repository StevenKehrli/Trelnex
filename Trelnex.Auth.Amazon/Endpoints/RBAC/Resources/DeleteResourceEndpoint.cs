using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class DeleteResourceEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapDelete(
                "/resources",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeleteResourceRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeletesResource")
            .WithDescription("Deletes the specified resource")
            .WithTags("Resources");
    }

    public static async Task<IResult> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<DeleteResourceRequest>();

        // delete the resource
        await rbacRepository.DeleteResourceAsync(
            resourceName: resourceName!);

        return Results.Ok();
    }

    public class RequestParameters
    {
        [FromBody]
        public DeleteResourceRequest? Request { get; init; }
    }
}
