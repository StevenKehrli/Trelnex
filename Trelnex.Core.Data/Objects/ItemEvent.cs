using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Audit record for item operations.
/// </summary>
/// <remarks>
/// Captures metadata about item modifications.
/// Stored with TypeName = "event".
/// </remarks>
public record ItemEvent
    : BaseItem
{
    #region Public Properties

    /// <summary>
    /// Type of operation performed on the related item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("saveAction")]
    public SaveAction SaveAction { get; private init; } = SaveAction.UNKNOWN;

    /// <summary>
    /// Unique identifier of the related item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("relatedId")]
    public string RelatedId { get; private init; } = null!;

    /// <summary>
    /// Type name of the related item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("relatedTypeName")]
    public string RelatedTypeName { get; private init; } = null!;

    /// <summary>
    /// Collection of property changes, or null if not tracked.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("changes")]
    public PropertyChange[]? Changes { get; private init; } = null!;

    /// <summary>
    /// W3C trace context (Activity.Current?.Id).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("traceContext")]
    public string? TraceContext { get; private init; } = null!;

    /// <summary>
    /// W3C trace ID (Activity.Current?.TraceId).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("traceId")]
    public string? TraceId { get; private init; } = null!;

    /// <summary>
    /// W3C span ID (Activity.Current?.SpanId).
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("spanId")]
    public string? SpanId { get; private init; } = null!;

    #endregion

    #region Internal Methods

    /// <summary>
    /// Creates a new event to record an operation on an item.
    /// </summary>
    /// <param name="relatedItem">Item that was modified.</param>
    /// <param name="saveAction">Operation type performed.</param>
    /// <param name="changes">Property changes made, if any.</param>
    /// <returns>A new event with details about the operation.</returns>
    /// <typeparam name="TItem">Type of item this event relates to.</typeparam>
    /// <remarks>
    /// Creates a timestamped record with a unique ID.
    /// </remarks>
    internal static ItemEvent Create(
        BaseItem relatedItem,
        SaveAction saveAction,
        PropertyChange[]? changes)
    {
        var dateTimeOffset = (relatedItem.DeletedDateTimeOffset is not null)
            ? relatedItem.DeletedDateTimeOffset.Value
            : relatedItem.UpdatedDateTimeOffset;

        return new ItemEvent
        {
            // Generate a unique ID for the event
            // Format: EVENT^^{related.Id}^00000001
            Id = $"EVENT^^{relatedItem.Id}^{relatedItem.Version:X8}",
            PartitionKey = relatedItem.PartitionKey,

            TypeName = ReservedTypeNames.Event,

            Version = relatedItem.Version,

            CreatedDateTimeOffset = dateTimeOffset,
            UpdatedDateTimeOffset = dateTimeOffset,

            SaveAction = saveAction,
            RelatedId = relatedItem.Id,
            RelatedTypeName = relatedItem.TypeName,
            Changes = changes,

            TraceContext = Activity.Current?.Id,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
        };
    }

    #endregion
}
