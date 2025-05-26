namespace Trelnex.Core.Data;

/// <summary>
/// Executes the query and returns the results as an asynchronous stream.
/// </summary>
/// <param name="queryable">The queryable to execute.</param>
/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
/// <returns>An asynchronous stream of items.</returns>
internal delegate IAsyncEnumerable<TItem> QueryAsyncDelegate<TInterface, TItem>(
    IQueryable<TItem> queryable,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
