using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Trelnex.Core.Encryption;

/// <summary>
/// Provides AES-256-GCM encryption and decryption services with authenticated encryption and HKDF key derivation.
/// This implementation uses a 256-bit key, 96-bit IV, 128-bit authentication tag, and 128-bit HKDF salt.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AesGcmCipher"/> class with the specified configuration.
/// The service uses HKDF (HMAC-based Key Derivation Function) with SHA-256 for secure key derivation from the provided secret.
/// </remarks>
/// <param name="configuration">The AES-GCM encryption configuration containing the secret key and other settings.</param>
public class AesGcmCipher(
    AesGcmCipherConfiguration configuration)
    : IBlockCipher
{
    #region Private Static Fields

    private static readonly int _authenticationTagSizeInBits = 128; // 128 bits
    private static readonly int _hkdfSaltLengthInBytes = 16; // 128 bits
    private static readonly int _ivLengthInBytes = 12; // 96 bits
    private static readonly int _keyLengthInBytes = 32; // 256 bits

    private static readonly SecureRandom _secureRandom = new();

    #endregion

    #region Private Fields

    private readonly Lazy<int> _id = new(() =>
    {
        var hashCode = new HashCode();

        hashCode.Add(nameof(AesGcmCipher));
        hashCode.Add(configuration.Secret);

        return hashCode.ToHashCode();
    });

    #endregion Private Fields

    #region Public Properties

    /// <inheritdoc />
    public int Id => _id.Value;

    #endregion

    #region Public Methods

    /// <inheritdoc />
    /// <remarks>
    /// This implementation uses AES-256-GCM with authenticated decryption and caller-controlled block allocation.
    /// The ciphertext must contain the HKDF salt, IV, encrypted data, and authentication tag in that order.
    /// Format: [16-byte HKDF salt][12-byte IV][encrypted data + 16-byte auth tag]
    /// The allocateBlock function receives the required size for the decrypted output and returns a BlockBuffer where decryption results will be written.
    /// The startIndex parameter allows decryption to begin at a specific offset within the ciphertext array.
    /// </remarks>
    public BlockBuffer Decrypt(
        byte[] ciphertext,
        int startIndex,
        Func<int, BlockBuffer> allocateBlock)
    {
        // Extract the HKDF salt from combined data
        var hkdfSalt = new byte[_hkdfSaltLengthInBytes];
        Buffer.BlockCopy(
            src: ciphertext,
            srcOffset: startIndex,
            dst: hkdfSalt,
            dstOffset: 0,
            count: hkdfSalt.Length);

        // Extract the random IV from combined data
        var iv = new byte[_ivLengthInBytes];
        Buffer.BlockCopy(
            src: ciphertext,
            srcOffset: startIndex + hkdfSalt.Length,
            dst: iv,
            dstOffset: 0,
            count: iv.Length);

        // Derive the encryption key from the secret and salt using HKDF.
        var key = DeriveKey(
            secret: configuration.Secret,
            salt: hkdfSalt,
            keyLengthInBytes: _keyLengthInBytes);

        // Create an AES cipher in GCM mode (authenticated encryption)
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            key: new KeyParameter(key),
            macSize: _authenticationTagSizeInBits,
            nonce: iv);
        cipher.Init(false, parameters);

        // Calculate cipher block offset and length
        var cipherBlockOffset = startIndex + hkdfSalt.Length + iv.Length;
        var cipherBlockLength = ciphertext.Length - cipherBlockOffset;

        // Calculate output size and allocate the block buffer
        var outputSize = cipher.GetOutputSize(cipherBlockLength);
        var blockBuffer = allocateBlock(outputSize);

        // Decrypt directly into the buffer from the original ciphertext array
        blockBuffer.Offset += cipher.ProcessBytes(ciphertext, cipherBlockOffset, cipherBlockLength, blockBuffer.Buffer, blockBuffer.Offset);
        blockBuffer.Offset += cipher.DoFinal(blockBuffer.Buffer, blockBuffer.Offset);

        return blockBuffer;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This implementation uses AES-256-GCM with authenticated encryption and caller-controlled block allocation.
    /// Generates a random HKDF salt and IV for each encryption operation to ensure semantic security.
    /// Output format: [16-byte HKDF salt][12-byte IV][encrypted data + 16-byte auth tag]
    /// The allocateBlock function receives the total required size and returns a BlockBuffer where encryption results will be written.
    /// The startIndex parameter allows encryption to begin at a specific offset within the plaintext array.
    /// </remarks>
    public BlockBuffer Encrypt(
        byte[] plaintext,
        int startIndex,
        Func<int, BlockBuffer> allocateBlock)
    {
        // Generate a random salt for HKDF
        var hkdfSalt = new byte[_hkdfSaltLengthInBytes];
        _secureRandom.NextBytes(hkdfSalt);

        // Derive the encryption key from the secret and salt using HKDF.
        var key = DeriveKey(
            secret: configuration.Secret,
            salt: hkdfSalt,
            keyLengthInBytes: _keyLengthInBytes);

        // Generate a random IV for each encryption
        var iv = new byte[_ivLengthInBytes];
        _secureRandom.NextBytes(iv);

        // Create an AES cipher in GCM mode (authenticated encryption)
        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            key: new KeyParameter(key),
            macSize: _authenticationTagSizeInBits,
            nonce: iv);
        cipher.Init(true, parameters);

        // Calculate total output size: salt + IV + encrypted data with auth tag
        var cipherBlockSize = cipher.GetOutputSize(plaintext.Length - startIndex);
        var totalSize = hkdfSalt.Length + iv.Length + cipherBlockSize;

        // Allocate the block buffer
        var blockBuffer = allocateBlock(totalSize);

        // Write HKDF salt to the buffer
        Buffer.BlockCopy(
            src: hkdfSalt,
            srcOffset: 0,
            dst: blockBuffer.Buffer,
            dstOffset: blockBuffer.Offset,
            count: hkdfSalt.Length);
        blockBuffer.Offset += hkdfSalt.Length;

        // Write IV to the buffer
        Buffer.BlockCopy(
            src: iv,
            srcOffset: 0,
            dst: blockBuffer.Buffer,
            dstOffset: blockBuffer.Offset,
            count: iv.Length);
        blockBuffer.Offset += iv.Length;

        // Encrypt directly into the buffer
        blockBuffer.Offset += cipher.ProcessBytes(plaintext, startIndex, plaintext.Length - startIndex, blockBuffer.Buffer, blockBuffer.Offset);
        blockBuffer.Offset += cipher.DoFinal(blockBuffer.Buffer, blockBuffer.Offset);

        return blockBuffer;
    }

    /// <summary>
    /// Derives a cryptographic key from the provided secret and salt using HKDF with SHA-256.
    /// This method implements the HKDF (HMAC-based Key Derivation Function) as specified in RFC 5869.
    /// </summary>
    /// <param name="secret">The input keying material (secret) used as the basis for key derivation.</param>
    /// <param name="salt">The salt value used to strengthen the key derivation process. Should be random and unique.</param>
    /// <param name="keyLengthInBytes">The desired length of the derived key in bytes.</param>
    /// <returns>A cryptographically strong derived key of the specified length.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> or <paramref name="salt"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="keyLengthInBytes"/> is less than or equal to zero.</exception>
    private static byte[] DeriveKey(
        string secret,
        byte[] salt,
        int keyLengthInBytes)
    {
        // Convert the secret to a byte array using UTF-8 encoding.
        var secretBytes = Encoding.UTF8.GetBytes(secret);

        // Use HKDF to derive key
        var hkdf = new HkdfBytesGenerator(new Sha256Digest());
        var parameters = new HkdfParameters(
            ikm: secretBytes,
            salt: salt,
            info: null);
        hkdf.Init(parameters);

        // Generate the key
        var key = new byte[keyLengthInBytes];
        hkdf.GenerateBytes(key, 0, keyLengthInBytes);

        return key;
    }

    #endregion
}
