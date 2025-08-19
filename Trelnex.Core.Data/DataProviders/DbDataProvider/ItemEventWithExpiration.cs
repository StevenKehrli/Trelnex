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
    /// Gets the expiration time for automatic deletion of this event record.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("expireAtDateTimeOffset")]
    public DateTimeOffset? ExpireAtDateTimeOffset { get; private init; } = null;
}
