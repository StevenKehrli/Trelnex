namespace Trelnex.Core.Encryption;

/// <summary>
/// Defines the available encryption algorithms.
/// </summary>
public enum EncryptionAlgorithm
{
    Undefined,

    /// <summary>
    /// AES encryption in Galois/Counter Mode (GCM).
    /// Provides authenticated encryption with associated data (AEAD).
    /// </summary>
    AesGcm
}
