namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for asynchronously executing a queryable and streaming the results.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="queryable">The queryable to execute.</param>
/// <param name="cancellationToken">Token to cancel the query operation.</param>
/// <returns>An asynchronous enumerable of items from the query execution.</returns>
internal delegate IAsyncEnumerable<TItem> QueryAsyncDelegate<TItem>(
    IQueryable<TItem> queryable,
    CancellationToken cancellationToken)
    where TItem : BaseItem;
