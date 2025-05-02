namespace Trelnex.Core.Data;

/// <summary>
/// Request to save an item.
/// </summary>
/// <typeparam name="TInterface">Interface type for the item.</typeparam>
/// <typeparam name="TItem">Concrete item type.</typeparam>
/// <param name="Item">The item to save.</param>
/// <param name="Event">Associated event with context and metadata.</param>
/// <param name="SaveAction">Type of operation.</param>
/// <remarks>
/// Encapsulates information needed for a save operation.
/// </remarks>
public record SaveRequest<TInterface, TItem>(
    TItem Item,
    ItemEvent<TItem> Event,
    SaveAction SaveAction)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
