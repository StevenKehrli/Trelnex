namespace Trelnex.Core.Encryption;

/// <summary>
/// Provides encryption and decryption services using a primary block cipher and zero or more secondary block ciphers.
/// The service uses the primary cipher for all encryption operations and identifies the correct cipher
/// for decryption by reading the cipher ID from the encrypted data.
/// </summary>
public interface IBlockCipherService
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

/// <summary>
/// Provides encryption and decryption services using a primary block cipher and zero or more secondary block ciphers.
/// The service uses the primary cipher for all encryption operations and identifies the correct cipher
/// for decryption by reading the cipher ID from the encrypted data.
/// </summary>
/// <param name="primaryCipher">The primary cipher used for all encryption operations.</param>
/// <param name="secondaryCiphers">Optional secondary ciphers used for decryption during key rotation scenarios.</param>
public class BlockCipherService(
    IBlockCipher primaryCipher,
    IBlockCipher[]? secondaryCiphers = null)
     : IBlockCipherService
{
    private static readonly int _cipherIdSize = sizeof(int);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when no cipher is found with the specified ID.</exception>
    public byte[] Decrypt(
        byte[] cipherText)
    {
        // Extract cipher ID from the beginning of the data
        var cipherId = BitConverter.ToInt32(cipherText, 0);

        // Get the cipher with cipher ID
        var cipher = GetCipher(cipherId)
            ?? throw new InvalidOperationException($"No IBlockCipher found with id '0x{cipherId:X8}'.");

        // Use the cipher to decrypt starting after the cipher ID
        var blockBuffer = cipher.Decrypt(cipherText, _cipherIdSize, requiredSize =>
        {
            return new BlockBuffer(
                buffer: new byte[requiredSize],
                offset: 0);
        });

        return blockBuffer.Buffer;
    }

    /// <inheritdoc />
    public byte[] Encrypt(
        byte[] plainText)
    {
        // Use the new IBlockCipher Encrypt method with allocateBlock function
        var blockBuffer = primaryCipher.Encrypt(plainText, 0, requiredSize =>
        {
            return new BlockBuffer(
                buffer: new byte[_cipherIdSize + requiredSize],
                offset: _cipherIdSize);
        });

        // Write cipher ID at the beginning
        var cipherIdBytes = BitConverter.GetBytes(primaryCipher.Id);
        Buffer.BlockCopy(cipherIdBytes, 0, blockBuffer.Buffer, 0, _cipherIdSize);

        return blockBuffer.Buffer;
    }

    /// <summary>
    /// Gets the cipher instance that matches the specified cipher ID.
    /// </summary>
    /// <param name="cipherId">The cipher ID to find.</param>
    /// <returns>The matching cipher instance, or null if no cipher is found with the specified ID.</returns>
    private IBlockCipher? GetCipher(
        int cipherId)
    {
        if (primaryCipher.Id == cipherId) return primaryCipher;

        return secondaryCiphers?.FirstOrDefault(c => c.Id == cipherId);
    }
}
