using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for building and executing queries against a backing data store with LINQ-like syntax.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="IQueryCommand{TInterface}"/> interface provides a fluent API for constructing and
/// executing queries against a data store. It follows LINQ's deferred execution pattern, where query
/// construction is separate from query execution.
/// </para>
/// <para>
/// Key characteristics of this interface include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Fluent query construction with LINQ-style methods like <see cref="Where"/>, <see cref="OrderBy{TKey}"/>,
///       <see cref="Skip"/>, and <see cref="Take"/>
///     </description>
///   </item>
///   <item>
///     <description>
///       Deferred execution where queries are only executed when materialized via <see cref="ToAsyncEnumerable"/>
///     </description>
///   </item>
///   <item>
///     <description>
///       Type-safe query expressions that work with the interface type rather than the concrete implementation
///     </description>
///   </item>
///   <item>
///     <description>
///       Results as <see cref="IQueryResult{TInterface}"/> objects that provide both access to the items
///       and the ability to transition to update or delete operations
///     </description>
///   </item>
/// </list>
/// <para>
/// This interface enables complex data querying while maintaining the abstraction between
/// the interface type used in application code and the concrete implementation type used
/// for storage. Query expressions are automatically translated from the interface type to the
/// concrete type for execution.
/// </para>
/// <para>
/// All query operations automatically filter out deleted items (where <see cref="IBaseItem.IsDeleted"/> is true)
/// and only return items matching the provider's type name, ensuring that only relevant, active items
/// are included in query results.
/// </para>
/// <para>
/// Typical usage pattern:
/// </para>
/// <code>
/// // Create a query command
/// var queryCommand = commandProvider.Query();
///
/// // Build the query using the fluent API
/// queryCommand = queryCommand
///     .Where(i => i.Category == "Electronics")
///     .OrderBy(i => i.Price)
///     .Skip(10)
///     .Take(5);
///
/// // Execute the query and process results
/// await foreach (var result in queryCommand.ToAsyncEnumerable())
/// {
///     Console.WriteLine($"Found item: {result.Item.Name}");
///
///     // Optionally transition to update or delete
///     var updateCommand = result.Update();
///     updateCommand.Item.ViewCount++;
///     await updateCommand.SaveAsync(requestContext, CancellationToken.None);
/// }
/// </code>
/// </remarks>
/// <seealso cref="ICommandProvider{TInterface}"/>
/// <seealso cref="IQueryResult{TInterface}"/>
public interface IQueryCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Adds an ascending sort operation to the query based on a specified property or expression.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function expression that extracts the sort key from each item.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the sort operation added,
    /// enabling a fluent API for chaining additional query operations.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method adds an ascending sort operation to the query chain, similar to SQL's ORDER BY clause.
    /// It follows LINQ's deferred execution pattern where the sort is not performed until the
    /// query is executed via <see cref="ToAsyncEnumerable"/>.
    /// </para>
    /// <para>
    /// The <paramref name="keySelector"/> allows selecting any property or computed value from the
    /// item as the sort key. For example, to sort by a Name property:
    /// </para>
    /// <code>
    /// queryCommand.OrderBy(item => item.Name)
    /// </code>
    /// <para>
    /// For more complex sorting needs, you can:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Sort by computed values: <c>OrderBy(item => item.FirstName + " " + item.LastName)</c>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Chain with <see cref="OrderByDescending{TKey}"/> for multi-level sorting
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// Under the hood, this method automatically converts expressions from the interface type to
    /// the concrete implementation type, maintaining the abstraction between application code and storage.
    /// </para>
    /// </remarks>
    /// <seealso cref="OrderByDescending{TKey}"/>
    /// <seealso cref="ToAsyncEnumerable"/>
    IQueryCommand<TInterface> OrderBy<TKey>(
        Expression<Func<TInterface, TKey>> keySelector);

    /// <summary>
    /// Adds a descending sort operation to the query based on a specified property or expression.
    /// </summary>
    /// <typeparam name="TKey">The type of the key returned by the <paramref name="keySelector"/>.</typeparam>
    /// <param name="keySelector">A function expression that extracts the sort key from each item.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the sort operation added,
    /// enabling a fluent API for chaining additional query operations.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="keySelector"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method adds a descending sort operation to the query chain, similar to SQL's ORDER BY ... DESC clause.
    /// It follows LINQ's deferred execution pattern where the sort is not performed until the
    /// query is executed via <see cref="ToAsyncEnumerable"/>.
    /// </para>
    /// <para>
    /// The <paramref name="keySelector"/> allows selecting any property or computed value from the
    /// item as the sort key. For example, to sort by price in descending order (highest first):
    /// </para>
    /// <code>
    /// queryCommand.OrderByDescending(item => item.Price)
    /// </code>
    /// <para>
    /// This method is particularly useful for:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Showing newest items first (by date)</description>
    ///   </item>
    ///   <item>
    ///     <description>Displaying highest-rated or most expensive items first</description>
    ///   </item>
    ///   <item>
    ///     <description>Sorting by priority or importance in reverse order</description>
    ///   </item>
    /// </list>
    /// <para>
    /// As with <see cref="OrderBy{TKey}"/>, this method automatically converts expressions from
    /// the interface type to the concrete implementation type.
    /// </para>
    /// </remarks>
    /// <seealso cref="OrderBy{TKey}"/>
    /// <seealso cref="ToAsyncEnumerable"/>
    IQueryCommand<TInterface> OrderByDescending<TKey>(
        Expression<Func<TInterface, TKey>> keySelector);

    /// <summary>
    /// Adds a skip operation to the query to bypass a specified number of items and return the remaining items.
    /// </summary>
    /// <param name="count">The number of items to skip before returning the remaining items.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the skip operation added,
    /// enabling a fluent API for chaining additional query operations.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <remarks>
    /// <para>
    /// This method adds a skip operation to the query chain, similar to SQL's OFFSET clause.
    /// It follows LINQ's deferred execution pattern where the skip is not performed until the
    /// query is executed via <see cref="ToAsyncEnumerable"/>.
    /// </para>
    /// <para>
    /// Skip is primarily used for implementing paging or handling large result sets by skipping
    /// over a specific number of items. It is often combined with <see cref="Take"/> to create
    /// a specific page of results:
    /// </para>
    /// <code>
    /// // Get the third page of results with 20 items per page
    /// var pageSize = 20;
    /// var pageNumber = 3; // 1-based page number
    /// var itemsToSkip = (pageNumber - 1) * pageSize;
    ///
    /// queryCommand
    ///     .OrderBy(item => item.Name)
    ///     .Skip(itemsToSkip)
    ///     .Take(pageSize);
    /// </code>
    /// <para>
    /// When working with large datasets, it's recommended to always use ordering in conjunction
    /// with Skip to ensure consistent paging results.
    /// </para>
    /// </remarks>
    /// <seealso cref="Take"/>
    /// <seealso cref="OrderBy{TKey}"/>
    /// <seealso cref="ToAsyncEnumerable"/>
    IQueryCommand<TInterface> Skip(
        int count);

    /// <summary>
    /// Adds a take operation to limit the query to a specified number of items.
    /// </summary>
    /// <param name="count">The maximum number of items to return.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the take operation added,
    /// enabling a fluent API for chaining additional query operations.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    /// <remarks>
    /// <para>
    /// This method adds a take operation to the query chain, similar to SQL's LIMIT or TOP clause.
    /// It follows LINQ's deferred execution pattern where the limit is not applied until the
    /// query is executed via <see cref="ToAsyncEnumerable"/>.
    /// </para>
    /// <para>
    /// Take is useful for:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Limiting result set size to improve performance</description>
    ///   </item>
    ///   <item>
    ///     <description>Implementing paging when combined with <see cref="Skip"/></description>
    ///   </item>
    ///   <item>
    ///     <description>Retrieving just the top N items (e.g., "Top 10 bestsellers")</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Example usage for a "Top 5" scenario:
    /// </para>
    /// <code>
    /// queryCommand
    ///     .OrderByDescending(item => item.Rating)
    ///     .Take(5);  // Get only the 5 highest-rated items
    /// </code>
    /// <para>
    /// When using Take for paging, it's typically combined with Skip and an OrderBy operation:
    /// </para>
    /// <code>
    /// queryCommand
    ///     .OrderBy(item => item.Name)
    ///     .Skip(20)  // Skip the first 20 items (first page)
    ///     .Take(10); // Take the next 10 items (second page with page size 10)
    /// </code>
    /// </remarks>
    /// <seealso cref="Skip"/>
    /// <seealso cref="OrderBy{TKey}"/>
    /// <seealso cref="ToAsyncEnumerable"/>
    IQueryCommand<TInterface> Take(
        int count);

    /// <summary>
    /// Executes the query and returns the results as an asynchronous stream.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> of <see cref="IQueryResult{TInterface}"/> that can be
    /// enumerated asynchronously to process the query results one at a time.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method materializes the query by executing it against the backing data store
    /// and returning the results as an asynchronous stream. Unlike the query construction
    /// methods (Where, OrderBy, etc.), this method triggers actual data retrieval.
    /// </para>
    /// <para>
    /// The returned async enumerable:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Supports cancellation through the <paramref name="cancellationToken"/> parameter
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Returns results wrapped in <see cref="IQueryResult{TInterface}"/> objects that
    ///       provide both read-only access to the items and the ability to transition to
    ///       update or delete operations
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Can be consumed using C# 8.0's await foreach syntax
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// // Execute the query and process results asynchronously
    /// await foreach (var result in queryCommand.ToAsyncEnumerable(cancellationToken))
    /// {
    ///     // Access the item in read-only mode
    ///     Console.WriteLine($"Found item: {result.Item.Name}");
    ///
    ///     // Optionally transition to a save command
    ///     var updateCommand = result.Update();
    ///     updateCommand.Item.ViewCount++;
    ///     await updateCommand.SaveAsync(requestContext, cancellationToken);
    /// }
    /// </code>
    /// <para>
    /// Or you can convert the async enumerable to other collections:
    /// </para>
    /// <code>
    /// // Get all results as an array
    /// var results = await queryCommand.ToAsyncEnumerable().ToArrayAsync();
    ///
    /// // Get the first result or null if none
    /// var firstResult = await queryCommand.ToAsyncEnumerable().FirstOrDefaultAsync();
    /// </code>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <seealso cref="IQueryResult{TInterface}"/>
    /// <seealso cref="Where"/>
    IAsyncEnumerable<IQueryResult<TInterface>> ToAsyncEnumerable(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a filter condition to the query to include only items that match the specified criteria.
    /// </summary>
    /// <param name="predicate">A function expression that defines the filter condition to test each item against.</param>
    /// <returns>
    /// The same <see cref="IQueryCommand{TInterface}"/> instance with the filter condition added,
    /// enabling a fluent API for chaining additional query operations.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method adds a filter condition to the query chain, similar to SQL's WHERE clause.
    /// It follows LINQ's deferred execution pattern where the filtering is not performed until the
    /// query is executed via <see cref="ToAsyncEnumerable"/>.
    /// </para>
    /// <para>
    /// The <paramref name="predicate"/> parameter is an expression that defines the filter condition,
    /// returning <see langword="true"/> for items that should be included in the results and
    /// <see langword="false"/> for items that should be excluded. For example:
    /// </para>
    /// <code>
    /// // Simple equality filter
    /// queryCommand.Where(item => item.Status == "Active");
    ///
    /// // Compound filter with multiple conditions
    /// queryCommand.Where(item =>
    ///     item.Category == "Electronics" &&
    ///     item.Price >= 100 &&
    ///     item.InStock);
    ///
    /// // Filter with string operations
    /// queryCommand.Where(item => item.Name.Contains("Phone"));
    /// </code>
    /// <para>
    /// Multiple Where calls can be chained to create compound filters, which are combined with AND logic:
    /// </para>
    /// <code>
    /// queryCommand
    ///     .Where(item => item.Category == "Electronics")
    ///     .Where(item => item.Price >= 100);
    /// </code>
    /// <para>
    /// Under the hood, this method automatically converts expressions from the interface type to
    /// the concrete implementation type, maintaining the abstraction between application code and storage.
    /// </para>
    /// <para>
    /// Note: All queries in this system automatically filter out deleted items (where IsDeleted=true)
    /// and only include items matching the provider's type name, even before any Where filters are applied.
    /// </para>
    /// </remarks>
    /// <seealso cref="OrderBy{TKey}"/>
    /// <seealso cref="ToAsyncEnumerable"/>
    IQueryCommand<TInterface> Where(
        Expression<Func<TInterface, bool>> predicate);
}

/// <summary>
/// Implements the query command pattern for data retrieval with LINQ-style syntax.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// This class provides a concrete implementation of <see cref="IQueryCommand{TInterface}"/> that
/// bridges the gap between the interface-based programming model and the concrete implementation
/// types required for data storage operations.
/// </para>
/// <para>
/// Key features of this implementation include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Expression conversion from interface types to concrete types for storage operations
///     </description>
///   </item>
///   <item>
///     <description>
///       Fluent API for building queries with method chaining
///     </description>
///   </item>
///   <item>
///     <description>
///       Deferred execution pattern where queries are composed but not executed until materialized
///     </description>
///   </item>
///   <item>
///     <description>
///       Async enumeration of results with cancellation support
///     </description>
///   </item>
///   <item>
///     <description>
///       Results wrapped in query result objects that provide read-only access and command transitions
///     </description>
///   </item>
/// </list>
/// <para>
/// The class is designed to be storage-agnostic by delegating the actual query execution to the
/// provider-specific <paramref name="executeQueryable"/> function, which translates the LINQ
/// expressions to the appropriate query language for the backing data store.
/// </para>
/// <para>
/// This implementation follows a lightweight adapter pattern, where the class:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       Converts interface-based expressions to concrete-type expressions
///     </description>
///   </item>
///   <item>
///     <description>
///       Applies the converted expressions to build a queryable
///     </description>
///   </item>
///   <item>
///     <description>
///       Delegates execution to the provided function
///     </description>
///   </item>
///   <item>
///     <description>
///       Converts raw results back to strongly-typed query results
///     </description>
///   </item>
/// </list>
/// </remarks>
/// <param name="expressionConverter">
/// The expression converter responsible for translating LINQ expressions from interface type to concrete implementation type.
/// </param>
/// <param name="queryable">
/// The base queryable data source with pre-applied filters for type name and soft-deleted items.
/// </param>
/// <param name="executeQueryable">
/// A function that executes the queryable against the specific data store and retrieves the results.
/// </param>
/// <param name="convertToQueryResult">
/// A function that converts raw item instances to query results with read-only access and command transition capabilities.
/// </param>
/// <seealso cref="IQueryCommand{TInterface}"/>
/// <seealso cref="ExpressionConverter{TInterface, TItem}"/>
/// <seealso cref="IQueryResult{TInterface}"/>
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
