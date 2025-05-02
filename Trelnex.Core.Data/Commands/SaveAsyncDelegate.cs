namespace Trelnex.Core.Data;

/// <summary>
/// Represents an asynchronous delegate for saving a single item to a data store.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the item to be saved. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type to be saved. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="request">A <see cref="SaveRequest{TInterface, TItem}"/> containing the item to save and contextual metadata about the save operation.</param>
/// <param name="cancellationToken">A token to observe for cancellation requests during the save operation.</param>
/// <returns>A task representing the asynchronous save operation that resolves to the saved <typeparamref name="TItem"/> after it has been persisted.</returns>
/// <remarks>
/// <para>
/// This delegate is used internally by repositories and data access services to implement save operations.
/// It abstracts the persistence mechanism, allowing different storage providers to be used interchangeably.
/// </para>
/// <para>
/// The implementation should handle the appropriate save action (create, update, delete) based on the
/// <see cref="SaveAction"/> specified in the request. Typically, implementations will:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Apply business rule validations if needed (though most validation should occur before this delegate is called)
///     </description>
///   </item>
///   <item>
///     <description>
///       Update timestamp fields like <c>CreatedDate</c> and <c>UpdatedDate</c> as appropriate for the operation
///     </description>
///   </item>
///   <item>
///     <description>
///       Perform the actual persistence operation (INSERT, UPDATE, or soft DELETE) in the underlying data store
///     </description>
///   </item>
///   <item>
///     <description>
///       Generate appropriate <see cref="ItemEvent{TItem}"/> records to track the changes for audit purposes
///     </description>
///   </item>
/// </list>
/// <para>
/// This delegate is primarily invoked by the <see cref="SaveCommand{TInterface, TItem}"/> class when
/// its <c>ExecuteAsync</c> method is called. It represents the final step in the command execution pipeline
/// that performs the actual persistence operation.
/// </para>
/// </remarks>
/// <seealso cref="SaveCommand{TInterface, TItem}"/>
/// <seealso cref="SaveBatchAsyncDelegate{TInterface, TItem}"/>
/// <seealso cref="SaveRequest{TInterface, TItem}"/>
internal delegate Task<TItem> SaveAsyncDelegate<TInterface, TItem>(
    SaveRequest<TInterface, TItem> request,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
