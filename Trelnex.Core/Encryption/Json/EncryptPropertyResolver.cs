using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Trelnex.Core.Json;

namespace Trelnex.Core.Encryption;

/// <summary>
/// A JSON property resolver that assigns an <see cref="EncryptedJsonConverter{T}"/>
/// to properties decorated with <see cref="EncryptAttribute"/>. This enables encryption and decryption
/// of property values during JSON serialization and deserialization.
/// </summary>
/// <param name="blockCipherService">
/// The <see cref="IBlockCipherService"/> used for encryption and decryption of property values.
/// </param>
public class EncryptPropertyResolver(
    IBlockCipherService blockCipherService)
    : JsonPropertyResolver
{
    public override IList<JsonPropertyInfo> ConfigureProperties(
        IList<JsonPropertyInfo> properties)
    {
        // Configure encryption for properties marked with EncryptAttribute
        foreach (var property in properties)
        {
            // Check if the property has the EncryptAttribute
            var encryptAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(EncryptAttribute), true)
                .FirstOrDefault();

            // Skip properties without EncryptAttribute
            if (encryptAttribute is null) continue;

            // Create an EncryptedJsonConverter for the property's type
            var converterType = typeof(EncryptedJsonConverter<>).MakeGenericType(property.PropertyType);

            var converter = (Activator.CreateInstance(converterType, blockCipherService) as JsonConverter)!;

            // Assign the encryption converter to the property
            property.CustomConverter = converter;
        }

        return properties;
    }
}
