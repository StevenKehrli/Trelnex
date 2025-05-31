using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Represents the OpenID Connect discovery configuration document.
/// </summary>
/// <remarks>
/// This record models the standard OpenID Connect discovery document as defined by the
/// OpenID Connect Discovery 1.0 specification. This document is typically served at the
/// /.well-known/openid-configuration endpoint and provides clients with the necessary
/// information to interact with the token service.
///
/// The discovery document includes information about:
/// - The issuer identifier
/// - Endpoint URLs for various operations
/// - Supported cryptographic algorithms
/// - Supported response types and subject types
/// - Claims supported
///
/// This configuration enables dynamic client registration and configuration, reducing the
/// need for manual configuration when integrating with the token service.
///
/// For more information, see:
/// https://openid.net/specs/openid-connect-discovery-1_0.html
/// </remarks>
public record OpenIdConfiguration
{
    #region Public Properties

    /// <summary>
    /// Gets the issuer identifier for this OpenID Provider.
    /// </summary>
    /// <remarks>
    /// This value is a case-sensitive URL that uniquely identifies the token issuer.
    /// It is used for validating the 'iss' claim in JWT tokens and must exactly match
    /// the value in the tokens for successful validation.
    /// </remarks>
    [ConfigurationKeyName("issuer")]
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    /// <summary>
    /// Gets the URL of the OAuth 2.0 token endpoint.
    /// </summary>
    /// <remarks>
    /// This endpoint is used by clients to exchange an authorization grant for an access token,
    /// typically using the client credentials grant type for service-to-service authentication.
    /// The token endpoint is used to obtain tokens for accessing protected resources.
    /// </remarks>
    [ConfigurationKeyName("token_endpoint")]
    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    /// <summary>
    /// Gets the URL of the JSON Web Key Set (JWKS) document.
    /// </summary>
    /// <remarks>
    /// This endpoint provides the set of JSON Web Keys (JWK) that contain the public keys
    /// used to verify the signatures of JWT tokens issued by the token service.
    /// Clients can use this endpoint to dynamically retrieve the keys needed for token validation,
    /// supporting key rotation without client reconfiguration.
    /// </remarks>
    [ConfigurationKeyName("jwks_uri")]
    [JsonPropertyName("jwks_uri")]
    public required string JwksUri { get; init; }

    /// <summary>
    /// Gets the OAuth 2.0 response types supported by the token endpoint.
    /// </summary>
    /// <remarks>
    /// This array lists the response_type values that the token service supports.
    /// Common values include "code", "token", or "id_token token" depending on the
    /// grant types and flows supported by the service.
    /// </remarks>
    [ConfigurationKeyName("response_types_supported")]
    [JsonPropertyName("response_types_supported")]
    public required string[] ResponseTypesSupported { get; init; }

    /// <summary>
    /// Gets the subject identifier types supported by the OpenID Provider.
    /// </summary>
    /// <remarks>
    /// This array specifies the subject identifier types that the token service supports.
    /// Valid values are "pairwise" for privacy-preserving identifiers that are different
    /// for each client, and "public" for consistent identifiers across all clients.
    /// </remarks>
    [ConfigurationKeyName("subject_types_supported")]
    [JsonPropertyName("subject_types_supported")]
    public required string[] SubjectTypesSupported { get; init; }

    /// <summary>
    /// Gets the JWS signing algorithms supported for the ID token.
    /// </summary>
    /// <remarks>
    /// This array lists the JWS (JSON Web Signature) algorithms that the token service
    /// supports for signing ID tokens. Common values include "RS256", "ES256", "ES384", etc.
    /// In this implementation, ECDSA algorithms (ES256, ES384, ES512) are commonly used
    /// as they align with the AWS KMS key types used for signing.
    /// </remarks>
    [ConfigurationKeyName("id_token_signing_alg_values_supported")]
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public required string[] IdTokenSigningAlgValuesSupported { get; init; }

    #endregion
}
