using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.RBAC;

internal static class CreateResourceEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapPost(
                "/resources",
                HandleRequest)
            .RequirePermission<RBACPermission.RBACCreatePolicy>()
            .Accepts<CreateResourceRequest>(MediaTypeNames.Application.Json)
            .Produces<CreateResourceResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("CreateResource")
            .WithDescription("Creates a new resource")
            .WithTags("Resources");
    }

    public static async Task<CreateResourceResponse> HandleRequest(
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IResourceNameValidator resourceNameValidator,
        [AsParameters] RequestParameters parameters)
    {
        // validate the resource name
        (var vrResourceName, var resourceName) =
            resourceNameValidator.Validate(
                parameters.Request?.ResourceName);

        vrResourceName.ValidateOrThrow<CreateResourceRequest>();

        // create the resource
        await rbacRepository.CreateResourceAsync(
            resourceName: resourceName!);

        // return the resource
        return new CreateResourceResponse
        {
            ResourceName = resourceName!
        };
    }

    public class RequestParameters
    {
        [FromBody]
        public CreateResourceRequest? Request { get; init; }
    }
}
