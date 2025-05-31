using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.JWT;

namespace Trelnex.Auth.Amazon.Endpoints.JWT;

/// <summary>
/// Provides an endpoint for retrieving the OpenID Connect configuration used for discovery.
/// </summary>
/// <remarks>
/// This endpoint implements the OpenID Connect Discovery 1.0 specification, exposing metadata
/// about the authentication service including token endpoints, supported scopes, and
/// JSON Web Key Set location. It is available at the standard well-known URI
/// /.well-known/openid-configuration
/// </remarks>
internal static class GetOpenIdConfigurationEndpoint
{
    #region Public Static Methods

    /// <summary>
    /// Maps the OpenID Connect configuration endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// The endpoint is mapped to the standard well-known URI path for OpenID Connect discovery.
    /// It is excluded from API documentation as it's a standard OpenID Connect endpoint.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the OpenID Connect configuration endpoint to the application's routing pipeline.
        erb.MapGet(
                ".well-known/openid-configuration",
                HandleRequest)
            .Produces<OpenIdConfiguration>()
            .ExcludeFromDescription();
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Handles requests to the OpenID Connect configuration endpoint.
    /// </summary>
    /// <param name="jwtProviderRegistry">The registry containing OAuth/OpenID Connect configuration.</param>
    /// <returns>The OpenID Connect configuration metadata document.</returns>
    /// <remarks>
    /// Returns a configuration object that describes the authentication service capabilities,
    /// including authorization endpoints, token issuers, and supported authentication flows.
    /// This enables automatic discovery of service endpoints by OAuth 2.0 and OpenID Connect clients.
    /// </remarks>
    internal static OpenIdConfiguration HandleRequest(
        [FromServices] IJwtProviderRegistry jwtProviderRegistry)
    {
        // Return the OpenID Connect configuration from the JWT provider registry.
        return jwtProviderRegistry.OpenIdConfiguration;
    }

    #endregion
}
