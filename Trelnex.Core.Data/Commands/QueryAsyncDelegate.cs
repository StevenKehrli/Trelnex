namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for asynchronously executing a queryable and streaming the results.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="queryable">The queryable to execute.</param>
/// <returns>An asynchronous enumerable of items from the query execution.</returns>
internal delegate IAsyncEnumerable<IQueryResult<TItem>> QueryAsyncDelegate<TItem>(
    IQueryable<TItem> queryable)
    where TItem : BaseItem;
