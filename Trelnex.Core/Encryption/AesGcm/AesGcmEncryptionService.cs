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
/// Initializes a new instance of the <see cref="AesGcmEncryptionService"/> class with the specified configuration.
/// The service uses HKDF (HMAC-based Key Derivation Function) with SHA-256 for secure key derivation from the provided secret.
/// </remarks>
/// <param name="configuration">The AES-GCM encryption configuration containing the secret key and other settings.</param>
internal class AesGcmEncryptionService(
    AesGcmEncryptionConfiguration configuration)
    : IEncryptionService
{
    #region Private Static Fields

    private static readonly int _authenticationTagSizeInBits = 128; // 128 bits
    private static readonly int _hkdfSaltLengthInBytes = 16; // 128 bits
    private static readonly int _ivLengthInBytes = 12; // 96 bits
    private static readonly int _keyLengthInBytes = 32; // 256 bits

    private static readonly SecureRandom _secureRandom = new();

    #endregion

    /// <summary>
    /// Decrypts the specified ciphertext using AES-256-GCM with authenticated decryption.
    /// The ciphertext must contain the HKDF salt, IV, encrypted data, and authentication tag in that order.
    /// </summary>
    /// <param name="ciphertext">The ciphertext to decrypt, including the prepended HKDF salt and IV.</param>
    /// <returns>The decrypted plaintext as a byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ciphertext"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the ciphertext is too short to contain the required components.</exception>
    /// <exception cref="InvalidCipherTextException">Thrown when decryption fails due to invalid ciphertext or authentication tag verification failure.</exception>
    public byte[] Decrypt(
        byte[] ciphertext)
    {
        // Extract the HKDF salt from combined data
        var hkdfSalt = new byte[_hkdfSaltLengthInBytes];
        Array.Copy(
            sourceArray: ciphertext,
            sourceIndex: 0,
            destinationArray: hkdfSalt,
            destinationIndex: 0,
            length: hkdfSalt.Length);

        // Extract the random IV from combined data
        var iv = new byte[_ivLengthInBytes];
        Array.Copy(
            sourceArray: ciphertext,
            sourceIndex: hkdfSalt.Length,
            destinationArray: iv,
            destinationIndex: 0,
            length: iv.Length);

        // Extract ciphertext
        var cipherBlock = new byte[ciphertext.Length - _hkdfSaltLengthInBytes - iv.Length];
        Array.Copy(
            sourceArray: ciphertext,
            sourceIndex: hkdfSalt.Length + iv.Length,
            destinationArray: cipherBlock,
            destinationIndex: 0,
            length: cipherBlock.Length);

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

        // Process the data
        var size = cipher.GetOutputSize(cipherBlock.Length);
        var plaintextBlock = new byte[size];

        var offset = cipher.ProcessBytes(cipherBlock, 0, cipherBlock.Length, plaintextBlock, 0);
        offset += cipher.DoFinal(plaintextBlock, offset);

        return plaintextBlock;
    }

    /// <summary>
    /// Encrypts the specified plaintext using AES-256-GCM with authenticated encryption.
    /// Generates a random HKDF salt and IV for each encryption operation to ensure semantic security.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <returns>
    /// The encrypted ciphertext as a byte array, with the HKDF salt and IV prepended to the encrypted data.
    /// Format: [16-byte HKDF salt][12-byte IV][encrypted data + 16-byte auth tag]
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="plaintext"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when encryption fails due to cryptographic engine errors.</exception>
    public byte[] Encrypt(
        byte[] plaintext)
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

        // Process the data
        var size = cipher.GetOutputSize(plaintext.Length);
        var cipherBlock = new byte[size];

        var offset = cipher.ProcessBytes(plaintext, 0, plaintext.Length, cipherBlock, 0);
        offset += cipher.DoFinal(cipherBlock, offset);

        // Combine the HKDF salt, random IV and ciphertext
        var ciphertext = new byte[hkdfSalt.Length + iv.Length + cipherBlock.Length];

        Array.Copy(
            sourceArray: hkdfSalt,
            sourceIndex: 0,
            destinationArray: ciphertext,
            destinationIndex: 0,
            length: hkdfSalt.Length);

        Array.Copy(
            sourceArray: iv,
            sourceIndex: 0,
            destinationArray: ciphertext,
            destinationIndex: hkdfSalt.Length,
            length: iv.Length);

        Array.Copy(
            sourceArray: cipherBlock,
            sourceIndex: 0,
            destinationArray: ciphertext,
            destinationIndex: hkdfSalt.Length + iv.Length,
            length: cipherBlock.Length);

        return ciphertext;
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
}
