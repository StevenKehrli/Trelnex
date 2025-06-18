using System.Text;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Tests.Encryption;

/// <summary>
/// Contains tests for the <see cref="AesGcmCipher"/> class.
/// </summary>
[Category("Encryption")]
public class AesGcmCipherTests
{
    [Test]
    [Description("Verifies that different instances of AesGcmCipher produce different ciphertexts for the same plaintext, but can still decrypt each other's ciphertexts.")]
    public void Cipher_DifferentInstance()
    {
        // Define a secret key and a plaintext input string for testing.
        var sIn = "9910bcc0-cc49-4c9b-b804-fc6f05e5993f";
        var plaintextBytesIn = Encoding.UTF8.GetBytes(sIn);

        var cipherConfiguration = new AesGcmCipherConfiguration
        {
            Secret = "bdca5de6-c8d7-4095-99d4-bc7e62aec848"
        };

        // Create the first AesGcmCipher instance with the defined secret.
        var cipher1 = new AesGcmCipher(cipherConfiguration);
        // Encrypt the plaintext using the first cipher.
        var ciphertext1 = cipher1.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext using the first cipher.
        var plaintextBytesOutFromCipher1ciphertext1 = cipher1.Decrypt(ciphertext1);
        // Convert the decrypted bytes back to a string.
        var sOutFromCipher1ciphertext1 = Encoding.UTF8.GetString(plaintextBytesOutFromCipher1ciphertext1);

        // Create the second AesGcmCipher instance with the same secret.
        var cipher2 = new AesGcmCipher(cipherConfiguration);
        // Encrypt the plaintext using the second cipher.
        var ciphertext2 = cipher2.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext using the second cipher.
        var plaintextBytesOutFromCipher2ciphertext2 = cipher2.Decrypt(ciphertext2);
        // Convert the decrypted bytes back to a string.
        var sOutFromCipher2ciphertext2 = Encoding.UTF8.GetString(plaintextBytesOutFromCipher2ciphertext2);

        // Attempt to decrypt ciphertext2 using the first cipher.
        var plaintextBytesOutFromCipher1ciphertext2 = cipher1.Decrypt(ciphertext2);
        // Convert the decrypted bytes back to a string.
        var sOutFromCipher1ciphertext2 = Encoding.UTF8.GetString(plaintextBytesOutFromCipher1ciphertext2);

        // Attempt to decrypt ciphertext1 using the second cipher.
        var plaintextBytesOutFromCipher2ciphertext1 = cipher2.Decrypt(ciphertext1);
        // Convert the decrypted bytes back to a string.
        var sOutFromCipher2ciphertext1 = Encoding.UTF8.GetString(plaintextBytesOutFromCipher2ciphertext1);

        // Assert multiple conditions to verify the encryption and decryption process.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sIn, Is.EqualTo(sOutFromCipher1ciphertext1), "The decrypted text from cipher 1 using ciphertext 1 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOutFromCipher2ciphertext2), "The decrypted text from cipher 2 using ciphertext 2 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOutFromCipher1ciphertext2), "The decrypted text from cipher 1 using ciphertext 2 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOutFromCipher2ciphertext1), "The decrypted text from cipher 2 using ciphertext 1 should match the original input.");
            Assert.That(ciphertext1, Is.Not.EqualTo(ciphertext2), "Ciphertext 1 and Ciphertext 2 should not be equal, demonstrating different ciphertexts for different instances.");
        }
    }

    /// <summary>
    /// Verifies that the same instance of AesGcmCipher produces different ciphertexts for the same plaintext on multiple encryptions.
    /// </summary>
    [Test]
    [Description("Verifies that the same instance of AesGcmCipher produces different ciphertexts for the same plaintext on multiple encryptions.")]
    public void Cipher_SameInstance()
    {
        // Define a secret key and a plaintext input string for testing.
        var sIn = "12de9c30-3405-42d5-a417-cbb033e7997e";
        var plaintextBytesIn = Encoding.UTF8.GetBytes(sIn);

        var cipherConfiguration = new AesGcmCipherConfiguration
        {
            Secret = "ee956b63-a94a-40ee-8c99-ced8427083a6"
        };

        // Create an AesGcmCipher instance with the defined secret.
        var cipher = new AesGcmCipher(cipherConfiguration);

        // Encrypt the plaintext using the cipher instance.
        var ciphertext1 = cipher.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext and convert the decrypted bytes back to a string.
        var plaintextBytesOut1 = cipher.Decrypt(ciphertext1);
        var sOut1 = Encoding.UTF8.GetString(plaintextBytesOut1);

        // Encrypt the plaintext again using the same cipher instance.
        var ciphertext2 = cipher.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext and convert the decrypted bytes back to a string.
        var plaintextBytesOut2 = cipher.Decrypt(ciphertext2);
        var sOut2 = Encoding.UTF8.GetString(plaintextBytesOut2);

        // Assert multiple conditions to verify the encryption and decryption process.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sIn, Is.EqualTo(sOut1), "The decrypted text from ciphertext 1 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOut2), "The decrypted text from ciphertext 2 should match the original input.");
            Assert.That(ciphertext1, Is.Not.EqualTo(ciphertext2), "ciphertext 1 and ciphertext 2 should not be equal, demonstrating different ciphertexts for each encryption.");
        }
    }
}
