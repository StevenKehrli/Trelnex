namespace Trelnex.Core.Encryption;

/// <summary>
/// Provides encryption and decryption services using a primary cipher and zero or more secondary ciphers.
/// The service uses the primary cipher for all encryption operations and identifies the correct cipher
/// for decryption by reading the cipher ID from the encrypted data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Decrypts the given encrypted data by reading the cipher ID from the data
    /// and using the matching cipher from primary or secondary ciphers.
    /// </summary>
    /// <param name="cipherText">The encrypted data containing the cipher ID followed by the ciphertext.</param>
    /// <returns>The decrypted plaintext data.</returns>
    byte[] Decrypt(byte[] cipherText);

    /// <summary>
    /// Encrypts the given plaintext data using the primary cipher and prepends the cipher ID.
    /// </summary>
    /// <param name="plainText">The plaintext data to encrypt.</param>
    /// <returns>The encrypted data with the cipher ID prepended.</returns>
    byte[] Encrypt(byte[] plainText);
}
