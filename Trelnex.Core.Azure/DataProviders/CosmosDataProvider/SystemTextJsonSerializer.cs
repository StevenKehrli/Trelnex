using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using Azure.Core.Serialization;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Cosmos DB serializer implementation using System.Text.Json for JSON serialization.
/// </summary>
internal class SystemTextJsonSerializer : CosmosLinqSerializer
{
    #region Private Static Fields

    /// <summary>
    /// Default JSON serializer options for Cosmos DB.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    #endregion

    #region Private Fields

    // Azure JSON serializer wrapper for System.Text.Json
    private readonly JsonObjectSerializer _jsonObjectSerializer = new(_jsonSerializerOptions);

    #endregion

    #region Public Methods

    /// <summary>
    /// Deserializes a JSON stream into an object of the specified type.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the stream into.</typeparam>
    /// <param name="stream">Stream containing JSON data to deserialize.</param>
    /// <returns>Deserialized object of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
    public override T FromStream<T>(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Deserialize stream using Azure JSON serializer wrapper
        using (stream)
        {
            return (T)_jsonObjectSerializer.Deserialize(stream, typeof(T), default)!;
        }
    }

    /// <summary>
    /// Determines the JSON property name for a .NET member during Cosmos DB serialization.
    /// </summary>
    /// <param name="memberInfo">Member information for property or field being serialized.</param>
    /// <returns>JSON property name to use, or null for extension data members.</returns>
    public override string SerializeMemberName(
        MemberInfo memberInfo)
    {
        // Check if member is marked as extension data container
        var dataAttribute = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(true);
        if (dataAttribute is not null) return null!;

        // Use JsonPropertyName attribute if present, otherwise use member name
        var nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true);

        string memberName = string.IsNullOrEmpty(nameAttribute?.Name)
            ? memberInfo.Name
            : nameAttribute.Name;

        return memberName;
    }

    /// <summary>
    /// Serializes an object into a JSON stream.
    /// </summary>
    /// <typeparam name="T">Type of object to serialize.</typeparam>
    /// <param name="item">Object to serialize to JSON.</param>
    /// <returns>Stream containing serialized JSON data.</returns>
    public override Stream ToStream<T>(
        T item)
    {
        // Create memory stream for serialized output
        var memoryStream = new MemoryStream();

        // Serialize object to memory stream
        _jsonObjectSerializer.Serialize(memoryStream, item, item.GetType(), default);

        // Reset position for reading
        memoryStream.Position = 0;

        return memoryStream;
    }

    #endregion
}
