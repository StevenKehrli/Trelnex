using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents an event record that captures information about operations performed on items.
/// </summary>
public record ItemEvent
    : BaseItem
{
    #region Public Properties

    /// <summary>
    /// Gets the type of save operation that was performed.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("saveAction")]
    public SaveAction SaveAction { get; private init; } = SaveAction.UNKNOWN;

    /// <summary>
    /// Gets the unique identifier of the item that this event relates to.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("relatedId")]
    public string RelatedId { get; private init; } = null!;

    /// <summary>
    /// Gets the type name of the item that this event relates to.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("relatedTypeName")]
    public string RelatedTypeName { get; private init; } = null!;

    /// <summary>
    /// Gets the property changes that occurred during the operation, or null if no changes were tracked.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("changes")]
    public PropertyChange[]? Changes { get; private init; } = null!;

    /// <summary>
    /// Gets the W3C trace context identifier from the current activity.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("traceContext")]
    public string? TraceContext { get; private init; } = null!;

    /// <summary>
    /// Gets the W3C trace identifier from the current activity.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("traceId")]
    public string? TraceId { get; private init; } = null!;

    /// <summary>
    /// Gets the W3C span identifier from the current activity.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("spanId")]
    public string? SpanId { get; private init; } = null!;

    #endregion

    #region Internal Methods

    /// <summary>
    /// Creates a new event record for an operation performed on an item.
    /// </summary>
    /// <param name="relatedItem">The item that the operation was performed on.</param>
    /// <param name="saveAction">The type of operation that was performed.</param>
    /// <param name="changes">The property changes that occurred, or null if none.</param>
    /// <returns>A new event record with populated metadata.</returns>
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
            // Create unique event ID based on related item ID and version
            Id = $"EVENT^{relatedItem.Id}^{relatedItem.Version:X8}",
            PartitionKey = relatedItem.PartitionKey,

            TypeName = ReservedTypeNames.Event,

            Version = relatedItem.Version,

            CreatedDateTimeOffset = dateTimeOffset,
            UpdatedDateTimeOffset = dateTimeOffset,

            SaveAction = saveAction,
            RelatedId = relatedItem.Id,
            RelatedTypeName = relatedItem.TypeName,
            Changes = changes,

            // Capture current activity tracing information
            TraceContext = Activity.Current?.Id,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
        };
    }

    #endregion
}
