namespace Trelnex.Core.Data;

internal delegate Task<TItem> SaveAsyncDelegate<TInterface, TItem>(
    SaveRequest<TInterface, TItem> request,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
