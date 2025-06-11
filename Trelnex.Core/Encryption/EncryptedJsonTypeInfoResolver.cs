using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Trelnex.Core.Encryption;

/// <summary>
/// A custom <see cref="IJsonTypeInfoResolver"/> that adds a custom <see cref="JsonConverter"/>
/// for properties decorated with the <see cref="EncryptAttribute"/>.
/// </summary>
/// <param name="encryptionService">The <see cref="IEncryptionService"/> to use for encryption and decryption.</param>
public class EncryptedJsonTypeInfoResolver(
    IEncryptionService encryptionService)
    : IJsonTypeInfoResolver
{
    // Use the default resolver as a base
    private static readonly IJsonTypeInfoResolver _base = new DefaultJsonTypeInfoResolver();

    /// <summary>
    /// Gets the <see cref="JsonTypeInfo"/> for the given type.
    /// </summary>
    /// <param name="type">The type to get the <see cref="JsonTypeInfo"/> for.</param>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
    /// <returns>The <see cref="JsonTypeInfo"/> for the given type, or <see langword="null"/> if none can be created.</returns>
    public JsonTypeInfo? GetTypeInfo(
        Type type,
        JsonSerializerOptions options)
    {
        // Get the base type info
        var jsonTypeInfo = _base.GetTypeInfo(type, options);

        // If the base type info is null, return null
        if (jsonTypeInfo is null) return null;

        // If the type is not an object, return the base type info
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object) return jsonTypeInfo;

        // Iterate over the properties
        foreach (var property in jsonTypeInfo.Properties)
        {
            // Get the EncryptAttribute
            var encryptAttribute = property.AttributeProvider?
                .GetCustomAttributes(typeof(EncryptAttribute), true)
                .FirstOrDefault();

            // If the property doesn't have the EncryptAttribute, continue
            if (encryptAttribute is null) continue;

            // Create the converter type
            var converterType = typeof(EncryptedJsonConverter<>).MakeGenericType(property.PropertyType);

            // Create the converter instance
            var converter = (Activator.CreateInstance(converterType, encryptionService) as JsonConverter)!;

            // Set the custom converter
            property.CustomConverter = converter;
        }

        return jsonTypeInfo;
    }
}
