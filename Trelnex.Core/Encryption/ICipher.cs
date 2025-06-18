namespace Trelnex.Core.Encryption;

/// <summary>
/// Represents a cipher component used by encryption services.
/// Each encryption service maintains a primary cipher and zero or more secondary ciphers
/// for encryption/decryption operations and key rotation scenarios.
/// </summary>
public interface ICipher
{
    /// <summary>
    /// Gets the unique identifier for this cipher instance.
    /// Used to distinguish between different cipher configurations within an encryption service.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Decrypts the given encrypted data using this cipher's configuration.
    /// </summary>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <param name="startIndex">The starting index in the data array to begin decryption. Defaults to 0.</param>
    /// <returns>The decrypted plaintext data.</returns>
    byte[] Decrypt(
        byte[] ciphertext,
        int startIndex = 0);

    /// <summary>
    /// Encrypts the given plaintext data using this cipher's configuration.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="startIndex">The starting index in the data array to begin encryption. Defaults to 0.</param>
    /// <returns>The encrypted data.</returns>
    byte[] Encrypt(
        byte[] plaintext,
        int startIndex = 0);
}
