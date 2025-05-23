namespace Trelnex.Core.Data.Encryption;

/// <summary>
/// Interface for encryption services.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the given data.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <returns>The encrypted data.</returns>
    byte[] Encrypt(byte[] data);

    /// <summary>
    /// Decrypts the given data.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <returns>The decrypted data.</returns>
    byte[] Decrypt(byte[] data);
}
