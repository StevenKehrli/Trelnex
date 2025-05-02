namespace Trelnex.Core.Data;

/// <summary>
/// Represents a request to save an item of type <typeparamref name="TItem"/> in the data store.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the item to be saved. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type to be saved. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="Item">The item to be saved.</param>
/// <param name="Event">The event associated with this save operation, providing context and metadata.</param>
/// <param name="SaveAction">The type of save action to be performed (e.g., create, update, delete).</param>
/// <remarks>
/// This record encapsulates all the necessary information needed to process a save operation.
/// It combines the item to be saved with contextual information about the operation, allowing
/// for proper handling of the save request by repository or service implementations.
/// </remarks>
public record SaveRequest<TInterface, TItem>(
    TItem Item,
    ItemEvent<TItem> Event,
    SaveAction SaveAction)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
