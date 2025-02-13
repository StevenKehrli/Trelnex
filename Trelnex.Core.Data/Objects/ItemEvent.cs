using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents information regarding the item and the caller that invoked the save method.
/// </summary>
/// <typeparam name="TItem">The specified item type.</typeparam>
public sealed class ItemEvent<TItem>
    : BaseItem
    where TItem : BaseItem
{
    /// <summary>
    /// Creates a new instance of the <see cref="ItemEvent{TItem}"/>.
    /// </summary>
    /// <param name="related">The <see cref="TItem"/> item that generated this event.</param>
    /// <param name="saveAction">The save action that created this event.</param>
    /// <param name="changes">The changes on the <see cref="TItem"/> item that generated this event.</param>
    /// <param name="requestContext">The <see cref="IRequestContext"> that represents the caller that invoked the save method that generated this event.</param>
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

    /// <summary>
    /// The save action that created this event.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("saveAction")]
    public SaveAction SaveAction { get; private init; } = SaveAction.UNKNOWN;

    /// <summary>
    /// The unique identifier that identifies the related item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("relatedId")]
    public string RelatedId { get; private init; } = null!;

    /// <summary>
    /// The type name of the related item.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("relatedTypeName")]
    public string RelatedTypeName { get; private init; } = null!;

    /// <summary>
    /// The item property changes associated with this event.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("changes")]
    public PropertyChange[]? Changes { get; private init; } = null!;

    /// <summary>
    /// The context that generated this event.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName("context")]
    public ItemEventContext Context { get; private init; } = null!;
}
