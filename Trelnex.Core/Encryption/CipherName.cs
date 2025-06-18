namespace Trelnex.Core.Encryption;

/// <summary>
/// Identifies the supported cipher implementations used by encryption services.
/// Each cipher name corresponds to a specific ICipher implementation with its own
/// encryption algorithm and configuration.
/// </summary>
public enum CipherName
{
    /// <summary>
    /// Represents an undefined or unspecified cipher type.
    /// </summary>
    Undefined,

    /// <summary>
    /// AES encryption in Galois/Counter Mode (GCM).
    /// Provides authenticated encryption with associated data (AEAD).
    /// </summary>
    AesGcm
}
