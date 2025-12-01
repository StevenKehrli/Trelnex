using System.Linq.Expressions;
using Trelnex.Core.Disposables;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for building and executing queries against items.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface IQueryCommand<TItem>
    where TItem : BaseItem
{
    /// <summary>
    /// Adds an ascending sort operation to the query.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">Expression that selects the property to sort by.</param>
    /// <returns>The same query command instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keySelector is null.</exception>
    IQueryCommand<TItem> OrderBy<TKey>(
        Expression<Func<TItem, TKey>> keySelector);

    /// <summary>
    /// Adds a descending sort operation to the query.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="keySelector">Expression that selects the property to sort by.</param>
    /// <returns>The same query command instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when keySelector is null.</exception>
    IQueryCommand<TItem> OrderByDescending<TKey>(
        Expression<Func<TItem, TKey>> keySelector);

    /// <summary>
    /// Adds a skip operation to bypass a specified number of items.
    /// </summary>
    /// <param name="count">Number of items to skip.</param>
    /// <returns>The same query command instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
    IQueryCommand<TItem> Skip(
        int count);

    /// <summary>
    /// Adds a take operation to limit the number of items returned.
    /// </summary>
    /// <param name="count">Maximum number of items to return.</param>
    /// <returns>The same query command instance for method chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
    IQueryCommand<TItem> Take(
        int count);

    /// <summary>
    /// Executes the query and returns results as a lazy asynchronous enumerable.
    /// </summary>
    /// <returns>An asynchronous enumerable that yields query results lazily.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    IAsyncDisposableEnumerable<IQueryResult<TItem>> ToAsyncDisposableEnumerable();

    /// <summary>
    /// Executes the query and returns all results materialized into a disposable enumerable.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the query operation.</param>
    /// <returns>A task containing a disposable enumerable with all query results.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<IDisposableEnumerable<IQueryResult<TItem>>> ToDisposableEnumerableAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a filter condition to the query.
    /// </summary>
    /// <param name="predicate">Expression that defines the filter condition.</param>
    /// <returns>The same query command instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
    IQueryCommand<TItem> Where(
        Expression<Func<TItem, bool>> predicate);
}

/// <summary>
/// Implements query building and execution operations for items.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
internal class QueryCommand<TItem>(
    IQueryable<TItem> queryable,
    QueryAsyncDelegate<TItem> queryAsyncDelegate)
    : IQueryCommand<TItem>
    where TItem : BaseItem
{
    #region Public Methods

    /// <inheritdoc/>
    public IQueryCommand<TItem> OrderBy<TKey>(
        Expression<Func<TItem, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // Apply the ordering operation to the queryable
        queryable = queryable.OrderBy(keySelector);

        return this;
    }

    /// <inheritdoc/>
    public IQueryCommand<TItem> OrderByDescending<TKey>(
        Expression<Func<TItem, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // Apply the descending ordering operation to the queryable
        queryable = queryable.OrderByDescending(keySelector);

        return this;
    }

    /// <inheritdoc/>
    public IQueryCommand<TItem> Skip(
        int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        // Apply the skip operation to the queryable
        queryable = queryable.Skip(count);

        return this;
    }

    /// <inheritdoc/>
    public IQueryCommand<TItem> Take(
        int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        // Apply the take operation to the queryable
        queryable = queryable.Take(count);

        return this;
    }

    /// <inheritdoc/>
    public IAsyncDisposableEnumerable<IQueryResult<TItem>> ToAsyncDisposableEnumerable()
    {
        return queryAsyncDelegate(queryable).ToAsyncDisposableEnumerable();
    }

    /// <inheritdoc/>
    public async Task<IDisposableEnumerable<IQueryResult<TItem>>> ToDisposableEnumerableAsync(
        CancellationToken cancellationToken = default)
    {
        return await queryAsyncDelegate(queryable).ToDisposableEnumerableAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public IQueryCommand<TItem> Where(
        Expression<Func<TItem, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        // Apply the filter condition to the queryable
        queryable = queryable.Where(predicate);

        return this;
    }

    #endregion
}
