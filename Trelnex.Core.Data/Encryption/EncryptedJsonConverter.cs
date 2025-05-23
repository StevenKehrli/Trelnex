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
        // Check for null token
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default!;
        }

        // Read the encrypted string
        var encryptedString = reader.GetString()!;

        // Convert the encrypted string to a byte array
        var encryptedBytes = System.Text.Encoding.UTF8.GetBytes(encryptedString);

        // Decrypt the byte array
        var jsonBytes = encryptionService.Decrypt(encryptedBytes);

        // Convert the decrypted byte array back to a JSON string
        var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);

        return typeof(TProperty) == typeof(string)
            ? (TProperty)(jsonString as object)
            : JsonSerializer.Deserialize<TProperty>(jsonString, options)!;
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
        // Check if the value is null
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Serialize the value to a JSON string
        using var jsonDocument = JsonSerializer.SerializeToDocument(new { Value = value }, options);
        var jsonElement = jsonDocument.RootElement.GetProperty("Value");

        var jsonString = jsonElement.ValueKind switch
        {
            JsonValueKind.String => jsonElement.GetString()!,
            _ => jsonElement.GetRawText()
        };

        // Convert the JSON string to a byte array
        var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

        // Encrypt the byte array
        var encryptedBytes = encryptionService.Encrypt(jsonBytes);

        // Convert the encrypted byte array back to a JSON string
        var encryptedString = System.Text.Encoding.UTF8.GetString(encryptedBytes);

        // Write the encrypted string to the JSON writer
        writer.WriteStringValue(encryptedString);
    }
}
