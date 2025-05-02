using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Audit record for item operations.
/// </summary>
/// <typeparam name="TItem">Type of item this event relates to.</typeparam>
/// <remarks>
/// Captures metadata about item modifications.
/// Stored with TypeName = "event".
/// </remarks>
public sealed class ItemEvent<TItem>
    : BaseItem
    where TItem : BaseItem
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
    /// Information about the request that generated this event.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("context")]
    public ItemEventContext Context { get; private init; } = null!;

    #endregion

    #region Internal Methods

    /// <summary>
    /// Creates a new event to record an operation on an item.
    /// </summary>
    /// <param name="related">Item that was modified.</param>
    /// <param name="saveAction">Operation type performed.</param>
    /// <param name="changes">Property changes made, if any.</param>
    /// <param name="requestContext">Caller information.</param>
    /// <returns>A new event with details about the operation.</returns>
    /// <remarks>
    /// Creates a timestamped record with a unique ID.
    /// </remarks>
    internal static ItemEvent<TItem> Create(
        TItem related,
        SaveAction saveAction,
        PropertyChange[]? changes,
        IRequestContext requestContext)
    {
        var dateTimeUtcNow = DateTime.UtcNow;

        return new ItemEvent<TItem>
        {
            Id = Guid.NewGuid().ToString(),
            PartitionKey = related.PartitionKey,

            TypeName = ReservedTypeNames.Event,

            CreatedDate = dateTimeUtcNow,
            UpdatedDate = dateTimeUtcNow,

            SaveAction = saveAction,
            RelatedId = related.Id,
            RelatedTypeName = related.TypeName,
            Changes = changes,
            Context = ItemEventContext.Convert(requestContext),
        };
    }

    #endregion
}
