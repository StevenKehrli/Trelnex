using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for querying items in a backing data store with LINQ-like syntax.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// This interface provides a fluent API for building queries against a data source,
/// with methods that mirror LINQ's deferred execution pattern.
/// </remarks>
public interface IQueryCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Sorts a sequence of items in ascending order according to a specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract a key from each item.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> whose items are sorted in ascending order according to a key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method performs deferred execution and doesn't retrieve any data until the query is enumerated.
    /// </remarks>
    IQueryCommand<TInterface> OrderBy<TKey>(
        Expression<Func<TInterface, TKey>> keySelector);

    /// <summary>
    /// Sorts a sequence of items in descending order according to a specified key selector.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function to extract a key from each item.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> whose items are sorted in descending order according to a key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method performs deferred execution and doesn't retrieve any data until the query is enumerated.
    /// </remarks>
    IQueryCommand<TInterface> OrderByDescending<TKey>(
        Expression<Func<TInterface, TKey>> keySelector);

    /// <summary>
    /// Bypasses a specified number of items in a sequence and then returns the remaining items.
    /// </summary>
    /// <param name="count">The number of items to skip before returning the remaining items.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> that contains the items that occur after the specified index in the input sequence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <remarks>
    /// This method performs deferred execution and doesn't retrieve any data until the query is enumerated.
    /// </remarks>
    IQueryCommand<TInterface> Skip(
        int count);

    /// <summary>
    /// Returns a specified number of contiguous items from the start of a sequence.
    /// </summary>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> that contains the specified number of items from the start of the input sequence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <remarks>
    /// This method performs deferred execution and doesn't retrieve any data until the query is enumerated.
    /// </remarks>
    IQueryCommand<TInterface> Take(
        int count);

    /// <summary>
    /// Executes the query and returns the results as an async enumerable.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="IQueryResult{TInterface}"/> containing the query results.</returns>
    /// <remarks>
    /// This method triggers execution of the query and materializes the results.
    /// </remarks>
    IAsyncEnumerable<IQueryResult<TInterface>> ToAsyncEnumerable(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Filters a sequence of items based on a predicate.
    /// </summary>
    /// <param name="predicate">A function to test each item for a condition.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> that contains items from the input sequence that satisfy the condition specified by <paramref name="predicate"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method performs deferred execution and doesn't retrieve any data until the query is enumerated.
    /// </remarks>
    IQueryCommand<TInterface> Where(
        Expression<Func<TInterface, bool>> predicate);
}

/// <summary>
/// Implements the <see cref="IQueryCommand{TInterface}"/> interface to query items in a backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete item type that implements the interface and inherits from <see cref="BaseItem"/>.</typeparam>
/// <remarks>
/// This class translates operations on <typeparamref name="TInterface"/> to operations on <typeparamref name="TItem"/>
/// by using expression conversion to maintain the abstraction between the interface and implementation.
/// </remarks>
/// <param name="expressionConverter">The expression converter to translate between interface and implementation types.</param>
/// <param name="queryable">The queryable data source.</param>
/// <param name="executeQueryable">Function to execute the queryable and retrieve results.</param>
/// <param name="convertToQueryResult">Function to convert items to query results.</param>
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
