using System.Text;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Trelnex.Core.Data.Encryption
{
    /// <summary>
    /// Provides encryption and decryption services using AES in GCM mode with HKDF for key derivation.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        #region Private Static Fields

        private static readonly int _authenticationTagSizeInBits = 128; // 128 bits
        private static readonly int _hkdfSaltLengthInBytes = 16; // 128 bits
        private static readonly int _ivLengthInBytes = 12; // 96 bits
        private static readonly int _keyLengthInBytes = 32; // 256 bits

        private static readonly SecureRandom _secureRandom = new();

        #endregion

        #region Private Fields

        private readonly string _secret;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionService"/> class.
        /// </summary>
        /// <param name="secret">The secret used for key derivation.</param>
        private EncryptionService(
            string secret)
        {
            _secret = secret;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="EncryptionService"/> class.
        /// </summary>
        /// <param name="secret">The secret used for key derivation.</param>
        /// <returns>A new instance of the <see cref="EncryptionService"/> class.</returns>
        /// <exception cref="ArgumentException">Thrown when the secret is null or empty.</exception>
        public static EncryptionService Create(
            string secret)
        {
            ArgumentException.ThrowIfNullOrEmpty(secret);

            return new EncryptionService(secret);
        }

        /// <summary>
        /// Encrypts the given plaintext using AES in GCM mode.
        /// </summary>
        /// <param name="plaintext">The plaintext to encrypt.</param>
        /// <returns>The encrypted ciphertext.</returns>
        public byte[] Encrypt(
            byte[] plaintext)
        {
            // Generate a random salt for HKDF
            var hkdfSalt = new byte[_hkdfSaltLengthInBytes];
            _secureRandom.NextBytes(hkdfSalt);

            // Derive the encryption key from the secret and salt using HKDF.
            var key = DeriveKey(
                secret: _secret,
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
        /// Decrypts the given ciphertext using AES in GCM mode.
        /// </summary>
        /// <param name="ciphertext">The ciphertext to decrypt.</param>
        /// <returns>The decrypted plaintext.</returns>
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
                secret: _secret,
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
        /// Derives a key from the given secret and salt using HKDF.
        /// </summary>
        /// <param name="secret">The secret to use for key derivation.</param>
        /// <param name="salt">The salt to use for key derivation.</param>
        /// <param name="keyLengthInBytes">The length of the key to derive, in bytes.</param>
        /// <returns>The derived key.</returns>
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
}
