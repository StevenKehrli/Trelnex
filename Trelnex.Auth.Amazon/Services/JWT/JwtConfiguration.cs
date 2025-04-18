namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Represents the configuration for the JWT factory.
/// </summary>
internal record JwtConfiguration
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

    /// <summary>
    /// The expiration time of the JWT token in minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; }
}
