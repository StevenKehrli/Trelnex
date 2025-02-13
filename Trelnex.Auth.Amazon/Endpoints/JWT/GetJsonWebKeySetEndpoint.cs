using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.JWT;

namespace Trelnex.Auth.Amazon.Endpoints.JWT;

internal static class GetJsonWebKeySetEndpoint
{
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        erb.MapGet(
                ".well-known/jwks.json",
                HandleRequest)
            .Produces<JsonWebKeySet>()
            .ExcludeFromDescription();
    }

    internal static async Task<JsonWebKeySet> HandleRequest(
        [FromServices] IJwtProvider jwtProvider)
    {
        // get the json web key set
        // convert for serialization
        var jwks = new JsonWebKeySet
        {
            Keys = jwtProvider.JWKS.Keys
                .Select(key => new JsonWebKey
                {
                    Crv = key.Crv,
                    KeyId = key.KeyId,
                    Kty = key.Kty,
                    X = key.X,
                    Y = key.Y
                })
                .ToArray()
        };

        return await Task.FromResult(jwks);
    }

    internal record JsonWebKey
    {
        [JsonPropertyName("crv")]
        public string Crv { get; init; } = null!;

        [JsonPropertyName("kid")]
        public string KeyId { get; init; } = null!;

        [JsonPropertyName("kty")]
        public string Kty { get; init; } = null!;

        [JsonPropertyName("x")]
        public string X { get; init; } = null!;

        [JsonPropertyName("y")]
        public string Y { get; init; } = null!;
    }

    internal record JsonWebKeySet
    {
        [JsonPropertyName("keys")]
        public JsonWebKey[] Keys { get; init; } = null!;
    }
}
