using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Trelnex.Core.Disposables;

namespace Trelnex.Core.Data;

/// <summary>
/// LINQ-style query interface.
/// </summary>
/// <typeparam name="TInterface">Item interface type.</typeparam>
/// <remarks>
/// Fluent API for querying data with deferred execution.
/// </remarks>
public interface IQueryCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Adds ascending sort.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <param name="keySelector">Function that selects sort key.</param>
    /// <returns>Query command for chaining.</returns>
    /// <exception cref="ArgumentNullException">When keySelector is null.</exception>
    IQueryCommand<TInterface> OrderBy<TKey>(
        Expression<Func<TInterface, TKey>> keySelector);

    /// <summary>
    /// Adds descending sort.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <param name="keySelector">Function that selects sort key.</param>
    /// <returns>Query command for chaining.</returns>
    /// <exception cref="ArgumentNullException">When keySelector is null.</exception>
    IQueryCommand<TInterface> OrderByDescending<TKey>(
        Expression<Func<TInterface, TKey>> keySelector);

    /// <summary>
    /// Skips a specified number of items.
    /// </summary>
    /// <param name="count">Number of items to skip.</param>
    /// <returns>Query command for chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When count is negative.</exception>
    IQueryCommand<TInterface> Skip(
        int count);

    /// <summary>
    /// Adds a take operation.
    /// </summary>
    /// <param name="count">The maximum number of items to return.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the take operation added.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    IQueryCommand<TInterface> Take(
        int count);

    /// <summary>
    /// Executes the query and returns the results with lazy async enumeration and automatic disposal management.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IAsyncDisposableEnumerable{T}"/> that can be enumerated asynchronously.
    /// Items are materialized lazily as they are enumerated, and all enumerated items are automatically disposed when the enumerable is disposed.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method provides lazy async enumeration where items are materialized one by one as they are enumerated.
    /// Use this method when you want to minimize memory usage or when processing large result sets.
    /// The returned enumerable tracks all materialized items and disposes them when disposed.
    /// </remarks>
    IAsyncDisposableEnumerable<IQueryResult<TInterface>> ToAsyncDisposableEnumerable(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the results with automatic disposal management.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="Task{T}"/> that represents the asynchronous operation.
    /// The task result contains an <see cref="IDisposableEnumerable{T}"/> of <see cref="IQueryResult{TInterface}"/>.
    /// All items are materialized and available for immediate access with array-like indexing and count properties.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method eagerly materializes all query results into memory before returning.
    /// Use this method when you need immediate access to item count, indexing, or when the result set is known to be small.
    /// All materialized items are automatically disposed when the returned enumerable is disposed.
    /// </remarks>
    Task<IDisposableEnumerable<IQueryResult<TInterface>>> ToDisposableEnumerableAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a filter condition.
    /// </summary>
    /// <param name="predicate">A function expression that defines the filter condition.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the filter condition added.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    IQueryCommand<TInterface> Where(
        Expression<Func<TInterface, bool>> predicate);
}

/// <summary>
/// Implements the query command pattern.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items.</typeparam>
/// <remarks>
/// This class provides a concrete implementation of <see cref="IQueryCommand{TInterface}"/>.
/// </remarks>
internal class QueryCommand<TInterface, TItem>(
    ExpressionConverter<TInterface, TItem> expressionConverter,
    IQueryable<TItem> queryable,
    QueryAsyncDelegate<TInterface, TItem> queryAsyncDelegate,
    Func<TItem, IQueryResult<TInterface>> convertToQueryResult)
    : IQueryCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Public Methods

    /// <inheritdoc/>
    public IQueryCommand<TInterface> OrderBy<TKey>(
        Expression<Func<TInterface, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // Convert the predicate from TInterface to TItem
        // See: https://stackoverflow.com/questions/14932779/how-to-change-a-type-in-an-expression-tree/14933106#14933106
        var expression = expressionConverter.Convert(keySelector);

        // Add the predicate to the queryable
        queryable = queryable.OrderBy(expression);

        return this;
    }

    /// <inheritdoc/>
    public IQueryCommand<TInterface> OrderByDescending<TKey>(
        Expression<Func<TInterface, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);

        // Convert the predicate from TInterface to TItem
        var expression = expressionConverter.Convert(keySelector);

        // Add the predicate to the queryable
        queryable = queryable.OrderByDescending(expression);

        return this;
    }

    /// <inheritdoc/>
    public IQueryCommand<TInterface> Skip(
        int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        // Add the skip operation to the queryable
        queryable = queryable.Skip(count);

        return this;
    }

    /// <inheritdoc/>
    public IQueryCommand<TInterface> Take(
        int count)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        // Add the take operation to the queryable
        queryable = queryable.Take(count);

        return this;
    }

    /// <inheritdoc/>
    public IAsyncDisposableEnumerable<IQueryResult<TInterface>> ToAsyncDisposableEnumerable(
        CancellationToken cancellationToken = default)
    {
        // Create an async enumerable that lazily converts items as they're enumerated
        var asyncEnumerable = QueryAsync(cancellationToken);

        // Wrap in AsyncDisposableEnumerable for lazy enumeration with automatic disposal tracking
        return AsyncDisposableEnumerable<IQueryResult<TInterface>>.From(asyncEnumerable);
    }

    /// <inheritdoc/>
    public async Task<IDisposableEnumerable<IQueryResult<TInterface>>> ToDisposableEnumerableAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new List<IQueryResult<TInterface>>();

        // Create an async enumerable that lazily converts items as they're enumerated
        var asyncEnumerable = QueryAsync(cancellationToken);

        // Eagerly materialize all results into memory
        await foreach (var item in asyncEnumerable)
        {
            results.Add(item);
        }

        // Return as a disposable enumerable with array-like access and automatic disposal management
        return DisposableEnumerable<IQueryResult<TInterface>>.From(results);
    }

    /// <inheritdoc/>
    public IQueryCommand<TInterface> Where(
        Expression<Func<TInterface, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        // Convert the predicate from TInterface to TItem
        var expression = expressionConverter.Convert(predicate);

        // Add the predicate to the queryable
        queryable = queryable.Where(expression);

        return this;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates an async enumerable that executes the query and converts items lazily.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable of query results.</returns>
    /// <remarks>
    /// This private method encapsulates the query execution logic and is reused by both
    /// ToAsyncDisposableEnumerable and ToDisposableEnumerableAsync methods to ensure consistency.
    /// </remarks>
    private async IAsyncEnumerable<IQueryResult<TInterface>> QueryAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Execute the underlying query through the delegate and convert items as they arrive
        await foreach (var item in queryAsyncDelegate(queryable, cancellationToken))
        {
            // Convert each TItem to an IQueryResult<TInterface> wrapper and yield it
            yield return convertToQueryResult(item);
        }
    }

    #endregion
}
