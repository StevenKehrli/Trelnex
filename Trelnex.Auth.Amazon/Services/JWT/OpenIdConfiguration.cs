using System.Text.Json.Serialization;

namespace Trelnex.Auth.Amazon.Services.JWT;

public class OpenIdConfiguration
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
