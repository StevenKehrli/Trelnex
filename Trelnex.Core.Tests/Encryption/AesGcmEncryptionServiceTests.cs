using System.Text;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Tests.Encryption;

/// <summary>
/// Contains tests for the <see cref="AesGcmEncryptionService"/> class.
/// </summary>
[Category("Encryption")]
public class AesGcmEncryptionServiceTests
{
    /// <summary>
    /// Verifies that different instances of AesGcmEncryptionService produce different ciphertexts for the same plaintext,
    /// but can still decrypt each other's ciphertexts.
    /// </summary>
    [Test]
    [Description("Verifies that different instances of AesGcmEncryptionService produce different ciphertexts for the same plaintext, but can still decrypt each other's ciphertexts.")]
    public void EncryptionService_DifferentInstance()
    {
        // Define a secret key and a plaintext input string for testing.
        var sIn = "9910bcc0-cc49-4c9b-b804-fc6f05e5993f";
        var plaintextBytesIn = Encoding.UTF8.GetBytes(sIn);

        var aesGcmEncryptionConfiguration = new AesGcmEncryptionConfiguration
        {
            Secret = "bdca5de6-c8d7-4095-99d4-bc7e62aec848"
        };

        // Create the first AesGcmEncryptionService instance with the defined secret.
        var encryptionService1 = new AesGcmEncryptionService(aesGcmEncryptionConfiguration);
        // Encrypt the plaintext using the first service instance.
        var ciphertext1 = encryptionService1.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext using the first service instance.
        var plaintextBytesOutFromService1ciphertext1 = encryptionService1.Decrypt(ciphertext1);
        // Convert the decrypted bytes back to a string.
        var sOutFromService1ciphertext1 = Encoding.UTF8.GetString(plaintextBytesOutFromService1ciphertext1);

        // Create the second AesGcmEncryptionService instance with the same secret.
        var encryptionService2 = new AesGcmEncryptionService(aesGcmEncryptionConfiguration);
        // Encrypt the plaintext using the second service instance.
        var ciphertext2 = encryptionService2.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext using the second service instance.
        var plaintextBytesOutFromService2ciphertext2 = encryptionService2.Decrypt(ciphertext2);
        // Convert the decrypted bytes back to a string.
        var sOutFromService2ciphertext2 = Encoding.UTF8.GetString(plaintextBytesOutFromService2ciphertext2);

        // Attempt to decrypt ciphertext2 using the first service instance.
        var plaintextBytesOutFromService1ciphertext2 = encryptionService1.Decrypt(ciphertext2);
        // Convert the decrypted bytes back to a string.
        var sOutFromService1ciphertext2 = Encoding.UTF8.GetString(plaintextBytesOutFromService1ciphertext2);

        // Attempt to decrypt ciphertext1 using the second service instance.
        var plaintextBytesOutFromService2ciphertext1 = encryptionService2.Decrypt(ciphertext1);
        // Convert the decrypted bytes back to a string.
        var sOutFromService2ciphertext1 = Encoding.UTF8.GetString(plaintextBytesOutFromService2ciphertext1);

        // Assert multiple conditions to verify the encryption and decryption process.
        Assert.Multiple(() =>
        {
            Assert.That(sIn, Is.EqualTo(sOutFromService1ciphertext1), "The decrypted text from service 1 using ciphertext 1 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOutFromService2ciphertext2), "The decrypted text from service 2 using ciphertext 2 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOutFromService1ciphertext2), "The decrypted text from service 1 using ciphertext 2 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOutFromService2ciphertext1), "The decrypted text from service 2 using ciphertext 1 should match the original input.");
            Assert.That(ciphertext1, Is.Not.EqualTo(ciphertext2), "Ciphertext 1 and Ciphertext 2 should not be equal, demonstrating different ciphertexts for different instances.");
        });
    }

    /// <summary>
    /// Verifies that the same instance of AesGcmEncryptionService produces different ciphertexts for the same plaintext on multiple encryptions.
    /// </summary>
    [Test]
    [Description("Verifies that the same instance of AesGcmEncryptionService produces different ciphertexts for the same plaintext on multiple encryptions.")]
    public void EncryptionService_SameInstance()
    {
        // Define a secret key and a plaintext input string for testing.
        var sIn = "12de9c30-3405-42d5-a417-cbb033e7997e";
        var plaintextBytesIn = Encoding.UTF8.GetBytes(sIn);

        var aesGcmEncryptionConfiguration = new AesGcmEncryptionConfiguration
        {
            Secret = "ee956b63-a94a-40ee-8c99-ced8427083a6"
        };

        // Create an AesGcmEncryptionService instance with the defined secret.
        var encryptionService = new AesGcmEncryptionService(aesGcmEncryptionConfiguration);

        // Encrypt the plaintext using the service instance.
        var ciphertext1 = encryptionService.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext and convert the decrypted bytes back to a string.
        var plaintextBytesOut1 = encryptionService.Decrypt(ciphertext1);
        var sOut1 = Encoding.UTF8.GetString(plaintextBytesOut1);

        // Encrypt the plaintext again using the same service instance.
        var ciphertext2 = encryptionService.Encrypt(plaintextBytesIn);
        // Decrypt the ciphertext and convert the decrypted bytes back to a string.
        var plaintextBytesOut2 = encryptionService.Decrypt(ciphertext2);
        var sOut2 = Encoding.UTF8.GetString(plaintextBytesOut2);

        // Assert multiple conditions to verify the encryption and decryption process.
        Assert.Multiple(() =>
        {
            Assert.That(sIn, Is.EqualTo(sOut1), "The decrypted text from ciphertext 1 should match the original input.");
            Assert.That(sIn, Is.EqualTo(sOut2), "The decrypted text from ciphertext 2 should match the original input.");
            Assert.That(ciphertext1, Is.Not.EqualTo(ciphertext2), "Ciphertext 1 and Ciphertext 2 should not be equal, demonstrating different ciphertexts for each encryption.");
        });
    }
}
