namespace Trelnex.Core.Data;

/// <summary>
/// Represents an asynchronous delegate for saving a batch of items in a single operation.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the items to be saved. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type to be saved. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="partitionKey">The partition key that identifies the logical partition where the items will be stored.</param>
/// <param name="requests">An array of <see cref="SaveRequest{TInterface, TItem}"/> objects containing the items to be saved and their associated metadata.</param>
/// <param name="cancellationToken">A token to observe for cancellation requests during the save operation.</param>
/// <returns>
/// A task representing the asynchronous save operation that resolves to an array of <see cref="SaveResult{TInterface, TItem}"/> objects,
/// each containing the result of an individual item save operation.
/// </returns>
/// <remarks>
/// This delegate is used internally to implement batch save operations across different storage providers.
/// The batch operation allows for atomic or transactional processing of multiple items within the same partition.
/// Each save request in the batch will generate a corresponding save result in the returned array,
/// maintaining the same order as the input requests.
/// </remarks>
internal delegate Task<SaveResult<TInterface, TItem>[]> SaveBatchAsyncDelegate<TInterface, TItem>(
    string partitionKey,
    SaveRequest<TInterface, TItem>[] requests,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
