namespace Trelnex.Core.Data;

internal delegate Task<SaveResult<TInterface, TItem>[]> SaveBatchAsyncDelegate<TInterface, TItem>(
    string partitionKey,
    SaveRequest<TInterface, TItem>[] requests,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
