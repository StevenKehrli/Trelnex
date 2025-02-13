using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

public class ItemEventContext
{
    /// <summary>
    /// Converts the specified <see cref="IRequestContext"/> to a <see cref="ItemEventContext"/>.
    /// </summary>
    /// <param name="requestContext">The specified <see cref="IRequestContext"/>.</param>
    /// <returns></returns>
    internal static ItemEventContext Convert(
        IRequestContext context)
    {
        return new ItemEventContext
        {
            ObjectId = context.ObjectId,
            HttpTraceIdentifier = context.HttpTraceIdentifier,
            HttpRequestPath = context.HttpRequestPath,
        };
    }

    /// <summary>
    /// Gets the unique object ID associated with the ClaimsPrincipal for this request.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("objectId")]
    public string? ObjectId { get; private set; }

    /// <summary>
    /// Gets the unique identifier to represent this request in trace logs.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("httpTraceIdentifier")]
    public string? HttpTraceIdentifier { get; private set; }

    /// <summary>
    /// Gets the portion of the request path that identifies the requested resource.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("httpRequestPath")]
    public string? HttpRequestPath { get; private set; }
}
