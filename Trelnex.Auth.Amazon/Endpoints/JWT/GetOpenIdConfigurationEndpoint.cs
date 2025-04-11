using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.JWT;

namespace Trelnex.Auth.Amazon.Endpoints.JWT;

internal static class GetOpenIdConfigurationEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                ".well-known/openid-configuration",
                HandleRequest)
            .Produces<OpenIdConfiguration>()
            .ExcludeFromDescription();
    }

    internal static OpenIdConfiguration HandleRequest(
        [FromServices] IJwtProviderRegistry jwtProviderRegistry)
    {
        return jwtProviderRegistry.OpenIdConfiguration;
    }
}
