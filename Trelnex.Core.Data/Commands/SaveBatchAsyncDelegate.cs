namespace Trelnex.Core.Data;

/// <summary>
/// Represents an asynchronous delegate for saving a batch of items in a single atomic operation.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the items to be saved. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type to be saved. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="partitionKey">The partition key that identifies the logical partition where the items will be stored.</param>
/// <param name="requests">An array of <see cref="SaveRequest{TInterface, TItem}"/> objects containing the items to be saved and their associated metadata.</param>
/// <param name="cancellationToken">A token to observe for cancellation requests during the save operation.</param>
/// <returns>
/// A task representing the asynchronous save operation that resolves to an array of <see cref="SaveResult{TInterface, TItem}"/> objects,
/// each containing the result of an individual item save operation.
/// </returns>
/// <remarks>
/// <para>
/// This delegate is used internally to implement batch save operations across different storage providers.
/// The batch operation allows for atomic or transactional processing of multiple items within the same partition.
/// All items in the batch must share the same partition key to ensure transactional consistency.
/// </para>
/// <para>
/// Key characteristics of the batch save operation:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <strong>Atomicity:</strong> All items in the batch should succeed or fail together.
///       If one operation fails, the entire batch should be rolled back to maintain data consistency.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Result ordering:</strong> Each save request in the batch will generate a corresponding save 
///       result in the returned array, maintaining the same order as the input requests.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Transactional integrity:</strong> Implementations should ensure that all changes are 
///       applied as a single transaction where the underlying data store supports it.
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>Error handling:</strong> If an error occurs during processing, the implementation should
///       apply appropriate status codes to each result indicating success, failure, or dependency failure.
///     </description>
///   </item>
/// </list>
/// <para>
/// This delegate is primarily invoked by the <see cref="BatchCommand{TInterface, TItem}"/> class when
/// its <c>ExecuteAsync</c> method is called. It represents the final step in the batch command execution 
/// pipeline that performs the actual persistence operations.
/// </para>
/// </remarks>
/// <seealso cref="BatchCommand{TInterface, TItem}"/>
/// <seealso cref="SaveAsyncDelegate{TInterface, TItem}"/>
/// <seealso cref="SaveRequest{TInterface, TItem}"/>
/// <seealso cref="SaveResult{TInterface, TItem}"/>
internal delegate Task<SaveResult<TInterface, TItem>[]> SaveBatchAsyncDelegate<TInterface, TItem>(
    string partitionKey,
    SaveRequest<TInterface, TItem>[] requests,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
