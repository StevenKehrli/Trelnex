using System.Text.Json.Serialization;
using Trelnex.Core.Data;

namespace Trelnex.Core.Amazon.DataProviders;

internal record ItemEventWithExpiration : ItemEvent
{
    public ItemEventWithExpiration(
        ItemEvent itemEvent,
        long? expireAt = null) : base(itemEvent)
    {
        ExpireAt = expireAt;
    }

    /// <summary>
    /// Gets the Unix timestamp when this event record should be automatically deleted by DynamoDB TTL.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expireAt")]
    public long? ExpireAt { get; private init; } = null;
}
