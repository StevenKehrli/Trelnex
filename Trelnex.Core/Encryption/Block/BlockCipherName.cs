namespace Trelnex.Core.Encryption;

/// <summary>
/// Identifies the supported block cipher implementations used by encryption services.
/// Each block cipher name corresponds to a specific IBlockCipher implementation with its own
/// encryption algorithm and configuration.
/// </summary>
public enum BlockCipherName
{
    /// <summary>
    /// Represents an undefined or unspecified block cipher type.
    /// </summary>
    Undefined,

    /// <summary>
    /// AES encryption in Galois/Counter Mode (GCM).
    /// Provides authenticated encryption with associated data (AEAD).
    /// </summary>
    AesGcm
}
