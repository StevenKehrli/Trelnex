using System.Text.Json.Serialization;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents an audit record for operations performed on items within the system.
/// </summary>
/// <typeparam name="TItem">The type of item this event relates to.</typeparam>
/// <remarks>
/// <para>
/// The ItemEvent class captures metadata about modifications to items in the system,
/// functioning as an audit trail and event log. Each event includes information about:
/// </para>
/// <list type="bullet">
///   <item>What action was performed (create, update, delete)</item>
///   <item>Which item was affected (by ID and type)</item>
///   <item>What specific properties changed (if applicable)</item>
///   <item>Who performed the action and when</item>
/// </list>
/// <para>
/// These events are stored with <see cref="ReservedTypeNames.Event"/> as their type name
/// to distinguish them from regular items in the system.
/// </para>
/// </remarks>
/// <seealso cref="BaseItem"/>
/// <seealso cref="ReservedTypeNames"/>
public sealed class ItemEvent<TItem>
    : BaseItem
    where TItem : BaseItem
{
    #region Internal Methods

    /// <summary>
    /// Creates a new <see cref="ItemEvent{TItem}"/> instance to record an operation on an item.
    /// </summary>
    /// <param name="related">The item that was modified and generated this event.</param>
    /// <param name="saveAction">The type of operation performed (create, update, delete).</param>
    /// <param name="changes">The property changes made to the item, if any.</param>
    /// <param name="requestContext">Information about the caller that initiated the operation.</param>
    /// <returns>A new <see cref="ItemEvent{TItem}"/> instance with details about the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="related"/> or <paramref name="requestContext"/> is null.</exception>
    /// <remarks>
    /// This method creates a timestamped record with a unique ID that captures the state
    /// of an item modification operation. It uses the current UTC time for both creation and
    /// update timestamps.
    /// </remarks>
    internal static ItemEvent<TItem> Create(
        TItem related,
        SaveAction saveAction,
        PropertyChange[]? changes,
        IRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(related);
        ArgumentNullException.ThrowIfNull(requestContext);

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

    #region Properties

    /// <summary>
    /// Gets the type of operation that was performed on the related item.
    /// </summary>
    /// <value>
    /// One of the <see cref="SaveAction"/> enum values representing the operation type.
    /// </value>
    [JsonInclude]
    [JsonPropertyName("saveAction")]
    public SaveAction SaveAction { get; private init; } = SaveAction.UNKNOWN;

    /// <summary>
    /// Gets the unique identifier of the item that this event relates to.
    /// </summary>
    /// <value>
    /// A string containing the ID of the related item.
    /// </value>
    [JsonInclude]
    [JsonPropertyName("relatedId")]
    public string RelatedId { get; private init; } = null!;

    /// <summary>
    /// Gets the type name of the item that this event relates to.
    /// </summary>
    /// <value>
    /// A string containing the type name of the related item.
    /// </value>
    [JsonInclude]
    [JsonPropertyName("relatedTypeName")]
    public string RelatedTypeName { get; private init; } = null!;

    /// <summary>
    /// Gets the collection of property changes made to the item.
    /// </summary>
    /// <value>
    /// An array of <see cref="PropertyChange"/> objects representing the modifications,
    /// or <see langword="null"/> if no specific properties were tracked.
    /// </value>
    [JsonInclude]
    [JsonPropertyName("changes")]
    public PropertyChange[]? Changes { get; private init; } = null!;

    /// <summary>
    /// Gets information about the request that generated this event.
    /// </summary>
    /// <value>
    /// An <see cref="ItemEventContext"/> object containing details about the request context.
    /// </value>
    [JsonInclude]
    [JsonPropertyName("context")]
    public ItemEventContext Context { get; private init; } = null!;

    #endregion
}
