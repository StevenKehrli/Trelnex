namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for asynchronously saving multiple items in a batch operation.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="requests">Array of save requests to process in the batch.</param>
/// <param name="cancellationToken">Token to cancel the batch save operation.</param>
/// <returns>Array of save results corresponding to each request.</returns>
internal delegate Task<SaveResult<TItem>[]> SaveBatchAsyncDelegate<TItem>(
    SaveRequest<TItem>[] requests,
    CancellationToken cancellationToken)
    where TItem : BaseItem;
