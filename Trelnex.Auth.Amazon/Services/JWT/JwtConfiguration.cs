using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Represents the configuration for the JWT factory.
/// </summary>
internal record JwtConfiguration
{
    /// <summary>
    /// The OpenId configuration (connect discovery document).
    /// </summary>
    [ConfigurationKeyName("openid-configuration")]
    [JsonPropertyName("openid-configuration")]
    public required OpenIdConfiguration OpenIdConfiguration { get; set; }

    /// <summary>
    /// The KMS algorithms configuration.
    /// </summary>
    public required KMSAlgorithmConfiguration KMSAlgorithms { get; set; }

    /// <summary>
    /// The expiration time of the JWT token in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; }
}

internal class OpenIdConfiguration
{
    [ConfigurationKeyName("issuer")]
    [JsonPropertyName("issuer")]
    public required string Issuer { get; init; }

    [ConfigurationKeyName("token_endpoint")]
    [JsonPropertyName("token_endpoint")]
    public required string TokenEndpoint { get; init; }

    [ConfigurationKeyName("jwks_uri")]
    [JsonPropertyName("jwks_uri")]
    public required string JwksUri { get; init; }

    [ConfigurationKeyName("response_types_supported")]
    [JsonPropertyName("response_types_supported")]
    public required string[] ResponseTypesSupported { get; init; }

    [ConfigurationKeyName("subject_types_supported")]
    [JsonPropertyName("subject_types_supported")]
    public required string[] SubjectTypesSupported { get; init; }

    [ConfigurationKeyName("id_token_signing_alg_values_supported")]
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public required string[] IdTokenSigningAlgValuesSupported { get; init; }
}

internal record KMSAlgorithmConfiguration
{
    /// <summary>
    /// The default key to use for the signing algorithm.
    /// </summary>
    public required string DefaultKey { get; init; }

    /// <summary>
    /// The regional keys to use for the signing algorithm.
    /// </summary>
    public string[]? RegionalKeys { get; init; }

    /// <summary>
    /// The secondary keys to use for the signing algorithm.
    /// </summary>
    public string[]? SecondaryKeys { get; init; }
}
