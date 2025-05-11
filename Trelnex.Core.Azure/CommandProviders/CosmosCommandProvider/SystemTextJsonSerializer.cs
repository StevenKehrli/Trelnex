using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// Custom serializer for Cosmos DB using System.Text.Json.
/// </summary>
/// <remarks>Extends <see cref="CosmosLinqSerializer"/> for System.Text.Json serialization.</remarks>
/// <param name="options">The JSON serializer options.</param>
internal class SystemTextJsonSerializer(
    JsonSerializerOptions options)
    : CosmosLinqSerializer
{
    #region Private Fields

    /// <summary>
    /// The underlying JSON serializer.
    /// </summary>
    private readonly JsonObjectSerializer _jsonObjectSerializer = new(options);

    #endregion

    #region Public Methods

    /// <summary>
    /// Deserializes a stream into an object of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize into.</typeparam>
    /// <param name="stream">The stream containing the JSON data.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is <see langword="null"/>.</exception>
    public override T FromStream<T>(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Deserialize the stream using the underlying serializer.
        using (stream)
        {
            return (T)_jsonObjectSerializer.Deserialize(stream, typeof(T), default)!;
        }
    }

    /// <summary>
    /// Maps .NET member names to their corresponding JSON property names for Cosmos DB serialization.
    /// </summary>
    /// <param name="memberInfo">The reflection member info representing a property or field to be serialized.</param>
    /// <returns>The property name to use in the serialized JSON document, or <see langword="null"/> for extension data members.</returns>
    /// <remarks>Uses <see cref="JsonPropertyNameAttribute"/> or the member's declared name.</remarks>
    public override string SerializeMemberName(
        MemberInfo memberInfo)
    {
        // First check if this member is marked as an extension data container.
        var dataAttribute = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(true);
        if (dataAttribute is not null) return null!;

        // Look for an explicit property name override using JsonPropertyName attribute.
        var nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true);

        // Determine the name to use for serialization:
        string memberName = string.IsNullOrEmpty(nameAttribute?.Name)
            ? memberInfo.Name
            : nameAttribute.Name;

        return memberName;
    }

    /// <summary>
    /// Serializes an object into a stream.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="item">The object to serialize.</param>
    /// <returns>A stream containing the serialized JSON data.</returns>
    public override Stream ToStream<T>(
        T item)
    {
        // Create a memory stream to hold the serialized data.
        var memoryStream = new MemoryStream();

        // Serialize the input object to the memory stream.
        _jsonObjectSerializer.Serialize(memoryStream, item, item.GetType(), default);

        // Reset the stream position to the beginning.
        memoryStream.Position = 0;

        return memoryStream;
    }

    #endregion
}
