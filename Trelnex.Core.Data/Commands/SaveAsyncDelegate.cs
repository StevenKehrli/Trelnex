namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for asynchronously saving an item.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="request">Save request containing the item, event metadata, and operation type.</param>
/// <param name="cancellationToken">Token to cancel the save operation.</param>
/// <returns>The saved item returned from the storage operation.</returns>
internal delegate Task<TItem> SaveAsyncDelegate<TItem>(
    SaveRequest<TItem> request,
    CancellationToken cancellationToken)
    where TItem : BaseItem;
