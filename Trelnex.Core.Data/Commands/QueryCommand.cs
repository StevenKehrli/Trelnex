using System.Linq.Expressions;

namespace Trelnex.Core.Data;

/// <summary>
/// The class to query the items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface IQueryCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Sorts a sequence of items in ascending order.
    /// </summary>
    /// <param name="predicate">A function to extract a key from each item.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> whose items are sorted according to a key.</returns>
    IQueryCommand<TInterface> OrderBy<TKey>(
        Expression<Func<TInterface, TKey>> predicate);

    /// <summary>
    /// Sorts a sequence of items in descending order.
    /// </summary>
    /// <param name="predicate">A function to extract a key from each item.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> whose items are sorted in descending according to a key.</returns>
    IQueryCommand<TInterface> OrderByDescending<TKey>(
        Expression<Func<TInterface, TKey>> predicate);

    /// <summary>
    /// Bypasses a specified number of items in a sequence and then returns the remaining items.
    /// </summary>
    /// <param name="count">The number of items to skip before returning the remaining items.</param>
    /// <returns>The <see cref="IQueryCommand{TInterface}"/> that contains the items that occur after the specified index in the input sequence.</returns>
    IQueryCommand<TInterface> Skip(int count);

    /// <summary>
    /// Returns a specified number of contiguous items from the start of a sequence.
    /// </summary>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>The <see cref="IQueryCommand{TInterface}"/> that contains the specified number of items from the start of the input sequence.</returns>
    IQueryCommand<TInterface> Take(int count);

    /// <summary>
    /// Executes the query and returns the results as an async enumerable.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <returns>The <see cref="IAsyncEnumerable{IQueryResult{TInterface}}"/>.</returns>
    IAsyncEnumerable<IQueryResult<TInterface>> ToAsyncEnumerable();

    /// <summary>
    /// Filters a sequence of items based on a predicate.
    /// </summary>
    /// <param name="predicate">A function to test each item for a condition.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> that contains items from the input sequence that satisfy the condition specified by <paramref name="predicate" />.</returns>
    IQueryCommand<TInterface> Where(
        Expression<Func<TInterface, bool>> predicate);
}

/// <summary>
/// The class to query the items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The item type that inherits from the interface type.</typeparam>
internal abstract class QueryCommand<TInterface, TItem>(
    ExpressionConverter<TInterface, TItem> expressionConverter,
    IQueryable<TItem> queryable)
    : IQueryCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// Gets the underlying <see cref="IQueryable{TItem}"/>.
    /// </summary>
    protected IQueryable<TItem> GetQueryable() => queryable;

    /// <summary>
    /// Sorts a sequence of items in ascending order.
    /// </summary>
    /// <param name="predicate">A function to extract a key from each item.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> whose items are sorted according to a key.</returns>
    public IQueryCommand<TInterface> OrderBy<TKey>(
        Expression<Func<TInterface, TKey>> predicate)
    {
        // need to convert the predicate from TInterface to TItem
        // https://stackoverflow.com/questions/14932779/how-to-change-a-type-in-an-expression-tree/14933106#14933106
        var expression = expressionConverter.Convert(predicate);

        // add the predicate to the queryable
        queryable = queryable.OrderBy(expression);

        return this;
    }

    /// <summary>
    /// Sorts a sequence of items in descending order.
    /// </summary>
    /// <param name="predicate">A function to extract a key from each item.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> whose items are sorted in descending according to a key.</returns>
    public IQueryCommand<TInterface> OrderByDescending<TKey>(
        Expression<Func<TInterface, TKey>> predicate)
    {
        // need to convert the predicate from TInterface to TItem
        // https://stackoverflow.com/questions/14932779/how-to-change-a-type-in-an-expression-tree/14933106#14933106
        var expression = expressionConverter.Convert(predicate);

        // add the predicate to the queryable
        queryable = queryable.OrderByDescending(expression);

        return this;
    }

    /// <summary>
    /// Bypasses a specified number of items in a sequence and then returns the remaining items.
    /// </summary>
    /// <param name="count">The number of items to skip before returning the remaining items.</param>
    /// <returns>The <see cref="IQueryCommand{TInterface}"/> that contains the items that occur after the specified index in the input sequence.</returns>
    public IQueryCommand<TInterface> Skip(
        int count)
    {
        // add the skip to the queryable
        queryable = queryable.Skip(count);

        return this;
    }

    /// <summary>
    /// Returns a specified number of contiguous items from the start of a sequence.
    /// </summary>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>The <see cref="IQueryCommand{TInterface}"/> that contains the specified number of items from the start of the input sequence.</returns>
    public IQueryCommand<TInterface> Take(
        int count)
    {
        // add the take to the queryable
        queryable = queryable.Take(count);

        return this;
    }

    /// <summary>
    /// Executes the query and returns the results as an async enumerable.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <returns>The <see cref="IAsyncEnumerable{IQueryResult{TInterface}}"/>.</returns>
    public IAsyncEnumerable<IQueryResult<TInterface>> ToAsyncEnumerable()
    {
        return ExecuteAsync();
    }

    /// <summary>Filters a sequence of items based on a predicate.</summary>
    /// <param name="predicate">A function to test each item for a condition.</param>
    /// <returns>An <see cref="IQueryCommand{TInterface}"/> that contains items from the input sequence that satisfy the condition specified by <paramref name="predicate" />.</returns>
    public IQueryCommand<TInterface> Where(
        Expression<Func<TInterface, bool>> predicate)
    {
        // need to convert the predicate from TInterface to TItem
        // https://stackoverflow.com/questions/14932779/how-to-change-a-type-in-an-expression-tree/14933106#14933106
        var expression = expressionConverter.Convert(predicate);

        // add the predicate to the queryable
        queryable = queryable.Where(expression);

        return this;
    }

    /// <summary>
    /// Executes the query and returns the results as an async enumerable.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The <see cref="IAsyncEnumerable{IQueryResult{TItem}}"/>.</returns>
    protected abstract IAsyncEnumerable<IQueryResult<TInterface>> ExecuteAsync(
        CancellationToken cancellationToken = default);
}
