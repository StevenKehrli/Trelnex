using System.Text.Json;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data.Encryption;

/// <summary>
/// A custom JSON converter that encrypts and decrypts properties of type <typeparamref name="TProperty"/>.
/// </summary>
/// <typeparam name="TProperty">The type of the property to encrypt/decrypt.</typeparam>
internal class EncryptedJsonConverter<TProperty>(
    IEncryptionService encryptionService)
    : JsonConverter<TProperty>
{
    /// <summary>
    /// Reads and decrypts the JSON value.
    /// </summary>
    /// <param name="reader">The <see cref="Utf8JsonReader"/> to read from.</param>
    /// <param name="typeToConvert">The type to convert to.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>The decrypted value.</returns>
    public override TProperty Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        // Check for null token. If the token is null, return the default value for the type.
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default!;
        }

        // Read the encrypted Base64 string from the JSON.
        var encryptedString = reader.GetString()!;

        // Decrypt and deserialize the value using the encrypted JSON service.
        return EncryptedJsonService.DecryptFromBase64<TProperty>(
            encryptedString,
            encryptionService)!;
    }

    /// <summary>
    /// Writes and encrypts the JSON value.
    /// </summary>
    /// <param name="writer">The <see cref="Utf8JsonWriter"/> to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    public override void Write(
        Utf8JsonWriter writer,
        TProperty value,
        JsonSerializerOptions options)
    {
        // Check if the value is null. If so, write a null value and return.
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Serialize and encrypt the value to a Base64 string using the encrypted JSON service.
        var encryptedString = EncryptedJsonService.EncryptToBase64(
            value,
            encryptionService);

        // Write the encrypted Base64 string to the JSON writer.
        writer.WriteStringValue(encryptedString);
    }
}
