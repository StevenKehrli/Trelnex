using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace Trelnex.Core.Data;

/// <summary>
/// A custom serializer using System.Text.Json to be used by the CosmosClient.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SystemTextJsonSerializer"/>.
/// </remarks>
/// <param name="options">The <see cref="JsonSerializerOptions"/> to be used with <see cref="JsonObjectSerializer"/>.</param>
internal class SystemTextJsonSerializer(
    JsonSerializerOptions options)
    : CosmosLinqSerializer
{
    /// <summary>
    /// The <see cref="JsonObjectSerializer"/>.
    /// </summary>
    private readonly JsonObjectSerializer _serializer = new(options);

    public override T FromStream<T>(
        Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using (stream)
        {
            return (T)(_serializer.Deserialize(stream, typeof(T), default)!);
        }
    }

    public override Stream ToStream<T>(T input)
    {
        var ms = new MemoryStream();

        _serializer.Serialize(ms, input, input.GetType(), default);

        ms.Position = 0;

        return ms;
    }

    public override string SerializeMemberName(
        MemberInfo memberInfo)
    {
        var dataAttribute = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(true);
        if (dataAttribute is not null) return null!;

        var nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(true);

        string memberName = string.IsNullOrEmpty(nameAttribute?.Name)
            ? memberInfo.Name
            : nameAttribute.Name;

        return memberName;
    }
}
