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
    /// Gets the time-to-live in seconds for automatic deletion of this event record by Cosmos DB.
    /// </summary>
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("ttl")]
    public int? TimeToLive { get; private init; } = null;
}
