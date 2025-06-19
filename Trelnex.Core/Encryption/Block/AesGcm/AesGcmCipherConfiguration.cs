namespace Trelnex.Core.Encryption;

/// <summary>
/// Configuration settings for AES-GCM encryption.
/// </summary>
internal record AesGcmCipherConfiguration
{
    /// <summary>
    /// Gets or sets the secret used for key derivation.
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}
