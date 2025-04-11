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
