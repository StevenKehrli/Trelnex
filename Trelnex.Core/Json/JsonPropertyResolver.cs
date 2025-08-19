using System.Text.Json.Serialization.Metadata;

namespace Trelnex.Core.Json;

/// <summary>
/// Abstract base class for JSON property resolvers that customize JSON serialization metadata.
/// Implementations can modify, add, or remove properties from JSON type information to control
/// how objects are serialized and deserialized.
/// </summary>
public abstract class JsonPropertyResolver
{
    /// <summary>
    /// Configures the JSON properties for a type by modifying the provided property collection.
    /// This method allows implementations to customize serialization behavior by adding converters,
    /// changing property names, filtering properties, or other modifications.
    /// </summary>
    /// <param name="properties">The collection of JSON properties to configure.</param>
    /// <returns>A modified collection of JSON properties with the desired configuration applied.</returns>
    public abstract IList<JsonPropertyInfo> ConfigureProperties(
        IList<JsonPropertyInfo> properties);
}
