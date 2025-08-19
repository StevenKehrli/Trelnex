namespace Trelnex.Core.Data;

/// <summary>
/// Represents a request to save an item with associated event metadata.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="Item">The item to save.</param>
/// <param name="Event">Event record containing operation metadata and changes.</param>
/// <param name="SaveAction">The type of save operation to perform.</param>
public record SaveRequest<TItem>(
    TItem Item,
    ItemEvent? Event,
    SaveAction SaveAction)
    where TItem : BaseItem;
