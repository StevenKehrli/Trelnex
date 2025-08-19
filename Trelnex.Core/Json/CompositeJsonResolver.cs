using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Trelnex.Core.Json;

/// <summary>
/// A custom <see cref="IJsonTypeInfoResolver"/> that applies multiple property resolvers
/// to customize JSON serialization and deserialization behavior. Each resolver can modify
/// properties in sequence, allowing for composable configuration of JSON type information.
/// </summary>
/// <param name="resolvers">
/// An array of <see cref="JsonPropertyResolver"/> instances that will be applied in order
/// to configure JSON properties for serialization and deserialization.
/// </param>
public class CompositeJsonResolver(
    JsonPropertyResolver[] resolvers)
    : DefaultJsonTypeInfoResolver
{
    /// <summary>
    /// Customizes the JSON serialization metadata by applying each configured resolver
    /// to the properties in sequence. Only object types are processed.
    /// </summary>
    /// <param name="type">The type being serialized.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>
    /// A <see cref="JsonTypeInfo"/> instance with properties configured by all resolvers.
    /// </returns>
    public override JsonTypeInfo GetTypeInfo(
        Type type,
        JsonSerializerOptions options)
    {
        // Get the default type info from the base resolver.
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        // Only process object types; skip primitives, arrays, and other non-object types.
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object) return jsonTypeInfo;

        var properties = jsonTypeInfo.Properties.ToList() as IList<JsonPropertyInfo>;

        foreach (var resolver in resolvers)
        {
            // Apply each resolver to configure the properties
            properties = resolver.ConfigureProperties(properties);
        }

        // Replace the original properties with the configured ones
        jsonTypeInfo.Properties.Clear();
        foreach (var property in properties)
        {
            jsonTypeInfo.Properties.Add(property);
        }

        return jsonTypeInfo;
    }
}
