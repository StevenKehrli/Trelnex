using System.Text;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Tests.Encryption;

/// <summary>
/// Contains tests for the <see cref="EncryptionService"/> class.
/// </summary>
[Category("Encryption")]
public class EncryptionServiceTests
{
    [Test]
    [Description("Verifies that EncryptionService can decrypt the ciphertext with primary cipher.")]
    public void EncryptionService_WithPrimaryCipher()
    {
        // Define a secret key and a plaintext input string for testing.
        var sIn = "ae0da4e7-60f9-4cf2-9056-eda182b57553";
        var plaintextBytesIn = Encoding.UTF8.GetBytes(sIn);

        // Create the first AesGcmCipher instance with the defined secret.
        var cipherConfiguration1 = new AesGcmCipherConfiguration
        {
            Secret = "62bde36e-1662-49e4-ac40-a2237e760f3f"
        };

        var cipher1 = new AesGcmCipher(cipherConfiguration1);

        // Create the EncryptionService with primary ciphers.
        var encryptionService1 = new EncryptionService(cipher1);

        // Encrypt the plaintext using the primary cipher.
        var cipherText1 = encryptionService1.Encrypt(plaintextBytesIn);

        // Decrypt the ciphertext using the encryption service.
        var plaintextBytesOut = encryptionService1.Decrypt(cipherText1);
        // Convert the decrypted bytes back to a string.
        var sOut = Encoding.UTF8.GetString(plaintextBytesOut);

        Assert.That(sOut, Is.EqualTo(sIn), "The decrypted text should match the original input.");
    }

    [Test]
    [Description("Verifies that EncryptionService can decrypt the ciphertext with secondary cipher.")]
    public void EncryptionService_WithSecondaryCipher()
    {
        // Define a secret key and a plaintext input string for testing.
        var sIn = "ae0da4e7-60f9-4cf2-9056-eda182b57553";
        var plaintextBytesIn = Encoding.UTF8.GetBytes(sIn);

        // Create the first AesGcmCipher instance with the defined secret.
        var cipherConfiguration1 = new AesGcmCipherConfiguration
        {
            Secret = "62bde36e-1662-49e4-ac40-a2237e760f3f"
        };

        var cipher1 = new AesGcmCipher(cipherConfiguration1);

        // Create the secondary AesGcmCipher instance with the defined secret.
        var cipherConfiguration2 = new AesGcmCipherConfiguration
        {
            Secret = "40880330-00a6-4b8d-9149-ed4e1c9ee62f"
        };

        var cipher2 = new AesGcmCipher(cipherConfiguration2);

        // Create the EncryptionService with primary and secondary ciphers.
        var encryptionService1 = new EncryptionService(cipher1);
        var encryptionService2 = new EncryptionService(cipher2, [ cipher1 ]);

        // Encrypt the plaintext using the first cipher.
        var ciphertext = encryptionService1.Encrypt(plaintextBytesIn);

        // Decrypt the ciphertext using the second cipher.
        var plaintextBytesOut = encryptionService2.Decrypt(ciphertext);
        // Convert the decrypted bytes back to a string.
        var sOut = Encoding.UTF8.GetString(plaintextBytesOut);

        Assert.That(sOut, Is.EqualTo(sIn), "The decrypted text should match the original input.");
    }
}
