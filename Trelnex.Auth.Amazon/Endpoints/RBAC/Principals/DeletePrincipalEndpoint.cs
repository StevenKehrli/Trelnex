using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class DeletePrincipalEndpoint
{
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(DeletePrincipalRequest).Name}' is not valid.",
        new Dictionary<string, string[]>
        {
            { nameof(DeletePrincipalRequest.PrincipalId), new[] { "principalId is not valid." } }
        });

    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapDelete(
                "/principals",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACDeletePolicy>()
            .Accepts<DeletePrincipalRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("DeletesPrincipal")
            .WithDescription("Deletes the specified principal")
            .WithTags("Principals");
    }

    public static async Task<IResult> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [AsParameters] RequestParameters parameters)
    {
        // validate the principal id
        if (parameters.Request?.PrincipalId is null) throw _validationException;

        // delete the principal
        await rbacRepository.DeletePrincipalAsync(
            principalId: parameters.Request!.PrincipalId);

        return Results.Ok();
    }

    public class RequestParameters
    {
        [FromBody]
        public DeletePrincipalRequest? Request { get; init; }
    }
}
