using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Trelnex.Core.Encryption;

/// <summary>
/// A custom <see cref="IJsonTypeInfoResolver"/> that assigns an <see cref="EncryptedJsonConverter{T}"/>
/// to properties decorated with <see cref="EncryptAttribute"/>. This enables encryption and decryption
/// of property values during JSON serialization and deserialization.
/// </summary>
/// <param name="blockCipherService">
/// The <see cref="IBlockCipherService"/> used for encryption and decryption of property values.
/// </param>
public class EncryptPropertyResolver(
    IBlockCipherService blockCipherService)
    : DefaultJsonTypeInfoResolver
{
    /// <summary>
    /// Customizes the JSON serialization metadata by assigning an <see cref="EncryptedJsonConverter{T}"/>
    /// to properties marked with <see cref="EncryptAttribute"/>. Only object types are processed.
    /// </summary>
    /// <param name="type">The type being serialized.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>
    /// A <see cref="JsonTypeInfo"/> instance with custom converters assigned to encrypted properties.
    /// </returns>
    public override JsonTypeInfo GetTypeInfo(
        Type type,
        JsonSerializerOptions options)
    {
        // Get the default type info from the base resolver.
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        // Only process object types; skip primitives, arrays, and other non-object types.
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object) return jsonTypeInfo;

        // Iterate over each property to check for EncryptAttribute.
        foreach (var property in jsonTypeInfo.Properties)
        {
            // Check if the property has the EncryptAttribute.
            var encryptAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(EncryptAttribute), true)
                .FirstOrDefault();

            // If EncryptAttribute is not present, skip this property.
            if (encryptAttribute is null) continue;

            // Create an EncryptedJsonConverter for the property's type, passing blockCipherService.
            var converterType = typeof(EncryptedJsonConverter<>).MakeGenericType(property.PropertyType);

            var converter = (Activator.CreateInstance(converterType, blockCipherService) as JsonConverter)!;

            // Assign the custom converter to the property for encryption/decryption.
            property.CustomConverter = converter;
        }

        return jsonTypeInfo;
    }
}
