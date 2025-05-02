namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for saving multiple items in a single atomic operation.
/// </summary>
/// <typeparam name="TInterface">Interface type for the items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <param name="requests">Array of save requests.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Array of save results.</returns>
/// <remarks>
/// Implements batch operations with atomicity.
/// </remarks>
internal delegate Task<SaveResult<TInterface, TItem>[]> SaveBatchAsyncDelegate<TInterface, TItem>(
    SaveRequest<TInterface, TItem>[] requests,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
