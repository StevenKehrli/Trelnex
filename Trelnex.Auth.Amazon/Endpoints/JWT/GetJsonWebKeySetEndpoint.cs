using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Auth.Amazon.Services.JWT;

namespace Trelnex.Auth.Amazon.Endpoints.JWT;

/// <summary>
/// Provides an endpoint for retrieving the JSON Web Key Set (JWKS) used for JWT token validation.
/// </summary>
/// <remarks>
/// This endpoint exposes the public keys used for verifying JWT tokens issued by the authentication service.
/// It follows the standard JWKS format as defined in the OpenID Connect and IETF RFC specifications.
/// The endpoint is available at the well-known URI /.well-known/jwks.json
/// </remarks>
internal static class GetJsonWebKeySetEndpoint
{
    #region Public Static Methods

    /// <summary>
    /// Maps the JWKS endpoint to the application's routing pipeline.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <remarks>
    /// The endpoint is mapped to the standard well-known URI path for JWKS.
    /// It is excluded from API documentation as it's a standard OpenID Connect discovery endpoint.
    /// </remarks>
    public static void Map(
        IEndpointRouteBuilder erb)
    {
        // Map the JWKS endpoint to the application's routing pipeline.
        erb.MapGet(
                ".well-known/jwks.json",
                HandleRequest)
            .Produces<JsonWebKeySet>()
            .ExcludeFromDescription();
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Handles requests to the JWKS endpoint.
    /// </summary>
    /// <param name="jwtProviderRegistry">The registry containing all available JWT providers and their keys.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the JSON Web Key Set.</returns>
    /// <remarks>
    /// Retrieves all available JSON Web Keys from the provider registry and formats them according to the JWKS specification.
    /// Only the public portions of the keys are exposed.
    /// </remarks>
    internal static Task<JsonWebKeySet> HandleRequest(
        [FromServices] IJwtProviderRegistry jwtProviderRegistry)
    {
        // Get the JSON Web Key Set from the registry and convert for serialization.
        // Maps from JsonWebKey to our simplified format.
        var jwks = new JsonWebKeySet
        {
            Keys = jwtProviderRegistry.JWKS.Keys
                .Select(key => new JsonWebKey
                {
                    Crv = key.Crv,       // The curve name for ECC keys.
                    KeyId = key.KeyId,   // Unique identifier for the key.
                    Kty = key.Kty,       // Key type (e.g., "EC" for elliptic curve).
                    X = key.X,           // X coordinate for EC public key.
                    Y = key.Y            // Y coordinate for EC public key.
                })
                .ToArray()
        };

        // Return the JSON Web Key Set.
        return Task.FromResult(jwks);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Represents a JSON Web Key for use in the JWKS endpoint.
    /// </summary>
    /// <remarks>
    /// Contains the essential properties needed for JWT signature verification.
    /// This is a simplified version of the full JWK specification,
    /// including only the properties needed for EC keys.
    /// </remarks>
    internal record JsonWebKey
    {
        /// <summary>
        /// Gets or initializes the curve name for ECC keys (e.g., "P-256").
        /// </summary>
        [JsonPropertyName("crv")]
        public string Crv { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the key identifier (kid).
        /// </summary>
        /// <remarks>
        /// The kid is used to match the key in the JWKS with the key used to sign a specific JWT.
        /// </remarks>
        [JsonPropertyName("kid")]
        public string KeyId { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the key type (e.g., "EC" for Elliptic Curve).
        /// </summary>
        [JsonPropertyName("kty")]
        public string Kty { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the X coordinate for the EC public key.
        /// </summary>
        /// <remarks>
        /// The X coordinate is Base64URL encoded.
        /// </remarks>
        [JsonPropertyName("x")]
        public string X { get; init; } = null!;

        /// <summary>
        /// Gets or initializes the Y coordinate for the EC public key.
        /// </summary>
        /// <remarks>
        /// The Y coordinate is Base64URL encoded.
        /// </remarks>
        [JsonPropertyName("y")]
        public string Y { get; init; } = null!;
    }

    /// <summary>
    /// Represents a set of JSON Web Keys for use in the JWKS endpoint.
    /// </summary>
    /// <remarks>
    /// Contains an array of JSON Web Keys as defined in the JWK specification.
    /// This format allows clients to discover the keys used for JWT signature verification.
    /// </remarks>
    internal record JsonWebKeySet
    {
        /// <summary>
        /// Gets or initializes the array of JSON Web Keys in the key set.
        /// </summary>
        [JsonPropertyName("keys")]
        public JsonWebKey[] Keys { get; init; } = null!;
    }

    #endregion
}
