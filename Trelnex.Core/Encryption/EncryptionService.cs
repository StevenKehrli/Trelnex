namespace Trelnex.Core.Encryption;

/// <summary>
/// Provides encryption and decryption services using a primary cipher and zero or more secondary ciphers.
/// The service uses the primary cipher for all encryption operations and identifies the correct cipher
/// for decryption by reading the cipher ID from the encrypted data.
/// </summary>
/// <param name="primaryCipher">The primary cipher used for all encryption operations.</param>
/// <param name="secondaryCiphers">Optional secondary ciphers used for decryption during key rotation scenarios.</param>
public class EncryptionService(
    ICipher primaryCipher,
    ICipher[]? secondaryCiphers = null)
     : IEncryptionService
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when no cipher is found with the specified ID.</exception>
    public byte[] Decrypt(
        byte[] cipherText)
    {
        // Extract cipher ID from the beginning of the data
        var cipherId = BitConverter.ToInt32(cipherText, 0);

        // Get the cipher with cipher ID
        var cipher = GetCipher(cipherId)
            ?? throw new InvalidOperationException($"No ICipher found with id '0x{cipherId:X8}'.");

        // Use the cipher to decrypt starting after the cipher ID
        return cipher.Decrypt(cipherText, sizeof(int));
    }

    /// <inheritdoc />
    public byte[] Encrypt(
        byte[] plainText)
    {
        var cipherText = primaryCipher.Encrypt(plainText);

        var dst = new byte[sizeof(int) + cipherText.Length];

        var cipherId = BitConverter.GetBytes(primaryCipher.Id);

        Buffer.BlockCopy(
            src: cipherId,
            srcOffset: 0,
            dst: dst,
            dstOffset: 0,
            count: cipherId.Length);

        Buffer.BlockCopy(
            src: cipherText,
            srcOffset: 0,
            dst: dst,
            dstOffset: cipherId.Length,
            count: cipherText.Length);

        return dst;
    }

    /// <summary>
    /// Gets the cipher instance that matches the specified cipher ID.
    /// </summary>
    /// <param name="cipherId">The cipher ID to find.</param>
    /// <returns>The matching cipher instance, or null if no cipher is found with the specified ID.</returns>
    private ICipher? GetCipher(
        int cipherId)
    {
        if (primaryCipher.Id == cipherId) return primaryCipher;

        return secondaryCiphers?.FirstOrDefault(c => c.Id == cipherId);
    }
}
