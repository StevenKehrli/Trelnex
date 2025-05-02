using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
    /// Executes the query and returns the results as an asynchronous stream.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> of <see cref="IQueryResult{TInterface}"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    IAsyncEnumerable<IQueryResult<TInterface>> ToAsyncEnumerable(
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
    Func<IQueryable<TItem>, CancellationToken, IEnumerable<TItem>> executeQueryable,
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
    public async IAsyncEnumerable<IQueryResult<TInterface>> ToAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Execute the query and process each result
        foreach (var item in executeQueryable(queryable, cancellationToken))
        {
            // Check for cancellation before yielding each item
            cancellationToken.ThrowIfCancellationRequested();

            // Convert the TItem to an IQueryResult<TInterface> and yield it
            yield return await Task.FromResult(convertToQueryResult(item));
        }
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
}
