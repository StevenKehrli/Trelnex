using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines the contract for results returned from batch operations against the data store.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the items in the batch result. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="IBatchResult{TInterface}"/> interface represents the outcome of a single item operation
/// within a batch transaction. Each batch operation returns an array of these results, with one result
/// for each item included in the original batch.
/// </para>
/// <para>
/// The interface provides access to:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       The HTTP status code indicating success or failure of the operation for this specific item
///     </description>
///   </item>
///   <item>
///     <description>
///       The read result containing the processed item data, if the operation was successful
///     </description>
///   </item>
/// </list>
/// <para>
/// Common HTTP status codes in batch results include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <c>200 OK</c>: The operation succeeded for this item
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>400 Bad Request</c>: The item failed validation or had an invalid state
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>424 Failed Dependency</c>: The item was valid, but another item in the batch failed,
///       causing this operation to fail due to the atomic nature of the batch
///     </description>
///   </item>
/// </list>
/// <para>
/// Consumers of this interface should first check the <see cref="HttpStatusCode"/> to determine
/// if the operation succeeded before attempting to access the <see cref="ReadResult"/>, which
/// will be <see langword="null"/> for failed operations.
/// </para>
/// </remarks>
/// <seealso cref="IBatchCommand{TInterface}"/>
/// <seealso cref="IReadResult{TInterface}"/>
public interface IBatchResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the HTTP status code indicating the outcome of this specific item operation within the batch.
    /// </summary>
    /// <value>
    /// An <see cref="HttpStatusCode"/> representing the outcome of the operation for this item.
    /// Common values include:
    /// <list type="bullet">
    ///   <item><description><c>200 OK</c>: Operation succeeded</description></item>
    ///   <item><description><c>400 Bad Request</c>: Item validation or state error</description></item>
    ///   <item><description><c>424 Failed Dependency</c>: Failed due to another item's failure</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// This property should always be checked first to determine if the operation succeeded before
    /// attempting to access the <see cref="ReadResult"/> property, which may be <see langword="null"/>
    /// for failed operations.
    /// </para>
    /// <para>
    /// The HTTP status code follows standard HTTP conventions to provide semantic meaning
    /// for the outcome. For example, a <c>400 Bad Request</c> indicates an issue with the
    /// specific item, while a <c>424 Failed Dependency</c> indicates that this item would have
    /// been valid, but the operation failed due to another item in the batch failing.
    /// </para>
    /// </remarks>
    HttpStatusCode HttpStatusCode { get; }

    /// <summary>
    /// Gets the read-only result containing the processed item data, if the operation was successful.
    /// </summary>
    /// <value>
    /// An <see cref="IReadResult{TInterface}"/> containing the read-only processed item if the operation
    /// was successful (HTTP status code 200 OK); otherwise, <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides access to the item data after the batch operation has completed successfully.
    /// The returned <see cref="IReadResult{TInterface}"/> wraps the item with read-only access and
    /// validation capabilities.
    /// </para>
    /// <para>
    /// This property will be <see langword="null"/> when:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>The operation failed for this item (HTTP status code is not 200 OK)</description>
    ///   </item>
    ///   <item>
    ///     <description>Another item in the batch failed, causing this operation to fail as well</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Always check the <see cref="HttpStatusCode"/> property before attempting to access this property.
    /// </para>
    /// </remarks>
    /// <seealso cref="IReadResult{TInterface}"/>
    IReadResult<TInterface>? ReadResult { get; }
}

/// <summary>
/// Implements the result of a batch operation for a specific item within a batch transaction.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the items in the batch result. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type in the batch. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="httpStatusCode">The HTTP status code representing the outcome of the batch operation for this specific item.</param>
/// <param name="readResult">The read result containing the processed item, if the operation was successful; otherwise, <see langword="null"/>.</param>
/// <remarks>
/// <para>
/// This sealed record class provides an immutable implementation of <see cref="IBatchResult{TInterface}"/>.
/// It captures the outcome of a single operation within a batch transaction, containing both the
/// HTTP status code indicating success or failure, and the read result with the processed item data
/// for successful operations.
/// </para>
/// <para>
/// The implementation is deliberately simple and immutable, with all state provided through constructor
/// parameters, following the record pattern introduced in C# 9. This ensures thread safety and prevents
/// accidental state changes after the result is created.
/// </para>
/// <para>
/// The class follows these conventions:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       For successful operations (HTTP status code 200 OK), the <paramref name="readResult"/> parameter
///       contains the processed item wrapped in a read-only container.
///     </description>
///   </item>
///   <item>
///     <description>
///       For failed operations (any other HTTP status code), the <paramref name="readResult"/> parameter
///       should be <see langword="null"/>, indicating no available data.
///     </description>
///   </item>
/// </list>
/// </remarks>
/// <seealso cref="IBatchResult{TInterface}"/>
/// <seealso cref="IBatchCommand{TInterface}"/>
internal sealed class BatchResult<TInterface, TItem>(
    HttpStatusCode httpStatusCode,
    IReadResult<TInterface>? readResult)
    : IBatchResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Public Properties

    /// <inheritdoc/>
    public HttpStatusCode HttpStatusCode => httpStatusCode;

    /// <inheritdoc/>
    public IReadResult<TInterface>? ReadResult => readResult;

    #endregion
}
