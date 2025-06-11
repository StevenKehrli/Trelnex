using System.Text.Json;

namespace Trelnex.Core.Encryption;

/// <summary>
/// Provides static methods for encrypting and decrypting objects by combining JSON serialization with encryption services.
/// </summary>
public static class EncryptedJsonService
{
    /// <summary>
    /// Encrypts an object by serializing it to JSON, encrypting the bytes, and encoding as Base64.
    /// </summary>
    /// <typeparam name="T">The type of object to encrypt.</typeparam>
    /// <param name="value">The object to encrypt. Can be null.</param>
    /// <param name="encryptionService">The encryption service to use for encrypting the JSON bytes.</param>
    /// <returns>
    /// A Base64-encoded string containing the encrypted JSON data, or null if the input value is null.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionService"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the object cannot be serialized to JSON.</exception>
    public static string? EncryptToBase64<T>(
        T? value,
        IEncryptionService encryptionService)
    {
        // Check if the value is null. If so, return null.
        if (value is null) return null;

        // Serialize the object of type TProperty to a JSON string.
        var jsonString = JsonSerializer.Serialize(value);

        // Convert the JSON string to a byte array.
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

        // Encrypt the byte array using the encryption service.
        var encryptedBytes = encryptionService.Encrypt(jsonBytes);

        // Convert the encrypted byte array to a Base64 string.
        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts a Base64-encoded encrypted string and deserializes it back to the original object.
    /// </summary>
    /// <typeparam name="T">The type of object to decrypt and deserialize to.</typeparam>
    /// <param name="encryptedBase64">The Base64-encoded encrypted string to decrypt. Can be null or empty.</param>
    /// <param name="encryptionService">The encryption service to use for decrypting the data.</param>
    /// <returns>
    /// The decrypted and deserialized object of type <typeparamref name="T"/>, or the default value of <typeparamref name="T"/> if the input is null or empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionService"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="encryptedBase64"/> is not a valid Base64 string.</exception>
    /// <exception cref="JsonException">Thrown when the decrypted JSON cannot be deserialized to type <typeparamref name="T"/>.</exception>
    public static T? DecryptFromBase64<T>(
        string? encryptedBase64,
        IEncryptionService encryptionService)
    {
        // Check if the encryptedBase64 is null or empty.
        if (string.IsNullOrEmpty(encryptedBase64)) return default;

        // Convert the encrypted Base64 string to a byte array.
        var encryptedBytes = Convert.FromBase64String(encryptedBase64);

        // Decrypt the byte array using the encryption service.
        var jsonBytes = encryptionService.Decrypt(encryptedBytes);

        // Convert the decrypted byte array back to a JSON string.
        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);

        // Deserialize the JSON string into an object of type TProperty.
        return JsonSerializer.Deserialize<T>(jsonString);
    }
}
