namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Represents the configuration for JWT token generation and validation.
/// </summary>
/// <remarks>
/// This configuration record contains settings that control how JWT tokens are created,
/// including which AWS KMS keys are used for signing tokens, and the token expiration policy.
///
/// The configuration supports multiple signing keys to facilitate key rotation strategies
/// and regional key distribution for improved performance and availability.
/// </remarks>
internal record JwtConfiguration
{
    #region Public Properties

    /// <summary>
    /// Gets the default AWS KMS key ARN to use for signing JWT tokens.
    /// </summary>
    /// <remarks>
    /// This is the primary key used for signing JWT tokens. It is specified as an AWS KMS key ARN
    /// (Amazon Resource Name) that identifies the cryptographic key in AWS Key Management Service.
    /// For example: "arn:aws:kms:us-west-2:111122223333:key/1234abcd-12ab-34cd-56ef-1234567890ab".
    /// </remarks>
    public required string DefaultKey { get; init; }

    /// <summary>
    /// Gets the collection of regional AWS KMS key ARNs that can be used for signing.
    /// </summary>
    /// <remarks>
    /// Regional keys allow for distributing signing operations across different AWS regions,
    /// which can improve performance by reducing latency for token issuance based on the request origin.
    /// Each key is specified as an AWS KMS key ARN in a different region.
    /// These keys are used when the configured region matches the client's region.
    /// </remarks>
    public string[]? RegionalKeys { get; init; }

    /// <summary>
    /// Gets the collection of secondary AWS KMS key ARNs that can be used for signing.
    /// </summary>
    /// <remarks>
    /// Secondary keys provide a mechanism for key rotation strategies. By configuring secondary keys,
    /// the system can continue to validate tokens signed with previous keys while issuing new tokens
    /// with the current default key. This allows for smooth transitions during key rotation.
    /// </remarks>
    public string[]? SecondaryKeys { get; init; }

    /// <summary>
    /// Gets or sets the expiration time of the JWT token in minutes.
    /// </summary>
    /// <remarks>
    /// This value determines how long issued tokens remain valid after their creation.
    /// Once a token has expired, it will be rejected during validation.
    /// The expiration time is a balance between security (shorter times reduce risk)
    /// and user experience (longer times reduce authentication frequency).
    /// </remarks>
    public int ExpirationInMinutes { get; set; }

    #endregion
}
