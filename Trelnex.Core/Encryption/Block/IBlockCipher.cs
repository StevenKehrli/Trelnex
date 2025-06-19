namespace Trelnex.Core.Encryption;

/// <summary>
/// Defines the interface for block cipher implementations.
/// Block ciphers encrypt and decrypt data in fixed-size blocks, inheriting common cipher functionality from <see cref="ICipher"/>.
/// Each encryption service maintains a primary block cipher and zero or more secondary block ciphers
/// for encryption/decryption operations and key rotation scenarios.
/// </summary>
public interface IBlockCipher
{
    /// <summary>
    /// Gets the unique identifier for this cipher instance.
    /// This identifier is used to distinguish between different cipher configurations
    /// within an encryption service, regardless of whether the cipher is a block or stream cipher.
    /// </summary>
    /// <value>
    /// A unique integer identifier for this cipher instance.
    /// </value>
    int Id { get; }

    /// <summary>
    /// Decrypts the given encrypted data using this cipher's configuration with caller-controlled block allocation.
    /// </summary>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <param name="startIndex">The starting index in the ciphertext array to begin decryption. Defaults to 0.</param>
    /// <param name="allocateBlock">A function that allocates a <see cref="BlockBuffer"/> for the decrypted data.
    /// The function receives the required buffer size and returns a BlockBuffer with an allocated buffer and the offset where decryption should begin writing.</param>
    /// <returns>The <see cref="BlockBuffer"/> instance returned by the allocator. The cipher writes the decrypted data starting at the BlockBuffer's offset, and the returned BlockBuffer may have an updated offset reflecting the final position.</returns>
    BlockBuffer Decrypt(
        byte[] ciphertext,
        int startIndex,
        Func<int, BlockBuffer> allocateBlock);

    /// <summary>
    /// Encrypts the given plaintext data using this cipher's configuration with caller-controlled block allocation.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="startIndex">The starting index in the plaintext array to begin encryption. Defaults to 0.</param>
    /// <param name="allocateBlock">A function that allocates a <see cref="BlockBuffer"/> for the encrypted data.
    /// The function receives the required buffer size and returns a BlockBuffer with an allocated buffer and the offset where encryption should begin writing.</param>
    /// <returns>The <see cref="BlockBuffer"/> instance returned by the allocator. The cipher writes the encrypted data starting at the BlockBuffer's offset, and the returned BlockBuffer may have an updated offset reflecting the final position.</returns>
    BlockBuffer Encrypt(
        byte[] plaintext,
        int startIndex,
        Func<int, BlockBuffer> allocateBlock);
}
