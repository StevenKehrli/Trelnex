using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.CallerIdentity;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;
using Trelnex.Core.Amazon.Identity;
using Trelnex.Core.Api.Responses;
using Trelnex.Core.Identity;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.Token;

internal static class GetTokenEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapPost(
                "/oauth2/token",
                HandleRequest)
            .Accepts<GetTokenForm>(MediaTypeNames.Application.FormUrlEncoded)
            .DisableAntiforgery()
            .Produces<AccessToken>()
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status400BadRequest)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status401Unauthorized)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<HttpStatusCodeResponse>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetAccessToken")
            .WithDescription("Gets an access token using client credentials.")
            .WithTags("Auth");
    }

    internal static async Task<AccessToken> HandleRequest(
        [FromServices] IScopeValidator scopeValidator,
        [FromServices] ICallerIdentityProvider callerIdentityProvider,
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IJwtProvider jwtProvider,
        [AsParameters] GetTokenForm form)
    {
        // validate the form
        form.Validate().ValidateOrThrow<GetTokenForm>();

        // parse the resource and scope
        var (validationResult, resourceName, scopeName) = scopeValidator.Validate(form.Scope);
        validationResult.ValidateOrThrow("scope");

        // decode the clientSecret to the signature
        var signature = CallerIdentitySignature.Decode(form.ClientSecret);
        signature.Validate().ValidateOrThrow("client_secret");

        // get the caller identity
        var principalId = await callerIdentityProvider.GetAsync(signature.Region, signature.Headers);

        // validate the caller identity against the clientId
        if (principalId != form.ClientId)
        {
            throw new HttpStatusCodeException(HttpStatusCode.Unauthorized);
        }

        // get the principal membership
        var principalMembership = await rbacRepository.GetPrincipalMembershipAsync(
            principalId: principalId,
            resourceName: resourceName,
            scopeName: scopeName,
            cancellationToken: default);

        // create the jwt token
        var accessToken = jwtProvider.CreateToken(
            region: signature.Region,
            principalId: principalId,
            audience: resourceName,
            scopes: principalMembership.ScopeNames,
            roles: principalMembership.RoleNames);

        return accessToken;
    }
}
