using System.Net;
using System.Net.Mime;
using Amazon;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.CallerIdentity;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core;
using Trelnex.Core.Amazon.Identity;
using Trelnex.Core.Identity;
using Trelnex.Core.Validation;

namespace Trelnex.Auth.Amazon.Endpoints.Token;

/// <summary>
/// Provides an endpoint for obtaining JWT access tokens using the client credentials flow.
/// </summary>
/// <remarks>
/// This endpoint implements a version of the OAuth 2.0 client credentials flow for machine-to-machine
/// authentication, using AWS IAM for caller identity verification. The endpoint accepts form-encoded
/// requests and returns JWT tokens with configured scopes and roles based on the caller's identity.
/// </remarks>
internal static class GetTokenEndpoint
{
    #region Private Static Fields

    private static readonly IScopeValidator _scopeValidator = new ScopeValidator();

    /// <summary>
    /// Pre-configured validation exception for the request is not valid.
    /// </summary>
    private static readonly ValidationException _validationException = new(
        $"The '{typeof(GetTokenForm).Name}' is not valid.");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Maps the token endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// The endpoint is mapped to the standard OAuth 2.0 token path (/oauth2/token) and accepts
    /// form-encoded requests. It produces JWT access tokens and appropriate error responses
    /// for various failure conditions.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the token endpoint to the application's routing pipeline.
        erb.MapPost(
                "/oauth2/token",
                HandleRequest)
            .Accepts<GetTokenForm>(MediaTypeNames.Application.FormUrlEncoded)
            .DisableAntiforgery()
            .Produces<AccessToken>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)
            .WithName("GetAccessToken")
            .WithDescription("Gets an access token using client credentials.")
            .WithTags("Auth");
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Handles requests to the token endpoint, processing client credentials and generating JWT tokens.
    /// </summary>
    /// <param name="scopeValidator">The validator for parsing and validating scope strings.</param>
    /// <param name="callerIdentityProvider">The provider for verifying caller identity from request signatures.</param>
    /// <param name="rbacRepository">The repository for role-based access control information.</param>
    /// <param name="jwtProviderRegistry">The registry of JWT providers for token generation.</param>
    /// <param name="form">The token request form containing client credentials and scope.</param>
    /// <returns>An access token with appropriate claims based on the caller's identity and permissions.</returns>
    /// <exception cref="ValidationException">Thrown when request validation fails.</exception>
    /// <exception cref="HttpStatusCodeException">Thrown when authentication fails or other errors occur.</exception>
    /// <remarks>
    /// The request handling process follows these steps:
    /// 1. Validate the form data and scope format
    /// 2. Decode and validate the client secret (which contains a signed AWS request signature)
    /// 3. Verify the caller's identity using AWS IAM
    /// 4. Confirm the claimed client ID matches the verified identity
    /// 5. Retrieve the principal's role memberships for the requested resource and scope
    /// 6. Generate a JWT token with appropriate claims based on the principal's permissions
    /// </remarks>
    internal static async Task<AccessToken> HandleRequest(
        [FromServices] ICallerIdentityProvider callerIdentityProvider,
        [FromServices] IRBACRepository rbacRepository,
        [FromServices] IJwtProviderRegistry jwtProviderRegistry,
        [AsParameters] GetTokenForm form)
    {
        // Validate the request form data.
        form.Validate().ValidateOrThrow<GetTokenForm>();

        // Parse and validate the requested scope (format: "resource/scope").
        var (validationResult, resourceName, scopeName) = _scopeValidator.Validate(form.Scope);
        validationResult.ValidateOrThrow("scope");

        // Decode the client secret to retrieve the AWS request signature.
        var signature = CallerIdentitySignature.Decode(form.ClientSecret);
        signature.Validate().ValidateOrThrow("client_secret");

        // Verify the caller's identity using AWS IAM GetCallerIdentity.
        var principalId = await callerIdentityProvider.GetAsync(signature.Region, signature.Headers);

        // Ensure the verified identity matches the claimed client ID.
        if (principalId != form.ClientId)
        {
            // If the principal ID does not match the client ID, return an unauthorized status code.
            throw new HttpStatusCodeException(HttpStatusCode.Unauthorized);
        }

        // Retrieve the principal's role memberships for the requested resource and scope.
        var principalAccess = await rbacRepository.GetPrincipalAccessAsync(
            principalId: principalId,
            resourceName: resourceName,
            scopeName: scopeName,
            cancellationToken: default);

        // Get the appropriate JWT provider for the requested AWS region.
        var regionEndpoint = RegionEndpoint.GetBySystemName(signature.Region);
        var jwtProvider = jwtProviderRegistry.GetProvider(regionEndpoint);

        // Generate the JWT token with appropriate claims.
        var accessToken = jwtProvider.Encode(
            audience: resourceName,              // The resource is used as the audience claim.
            principalId: principalId,            // The principal ID is used for 'sub' and 'oid' claims.
            scopes: principalAccess.ScopeNames,  // The scopes are added as the 'scp' claim.
            roles: principalAccess.RoleNames);   // The roles are added as the 'roles' claim.

        // Return the access token.
        return accessToken;
    }

    #endregion
}
