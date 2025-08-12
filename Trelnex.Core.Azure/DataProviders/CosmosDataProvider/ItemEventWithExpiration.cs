using System.Text.Json.Serialization;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.DataProviders;

internal record ItemEventWithExpiration : ItemEvent
{
    public ItemEventWithExpiration(
        ItemEvent itemEvent,
        int? timeToLive = null) : base(itemEvent)
    {
        TimeToLive = timeToLive;
    }

    /// <summary>
    /// Optional time to live for the event.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; private init; } = null;
}
