using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

internal record ItemEventWithExpiration : ItemEvent
{
    public ItemEventWithExpiration(
        ItemEvent itemEvent,
        DateTimeOffset? expireAtDateTimeOffset = null) : base(itemEvent)
    {
        ExpireAtDateTimeOffset = expireAtDateTimeOffset;
    }

    /// <summary>
    /// Optional time to live for the event.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expireAtDateTimeOffset")]
    public DateTimeOffset? ExpireAtDateTimeOffset { get; private init; } = null;
}
