using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for accessing and transitioning items retrieved from query operations.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="IQueryResult{TInterface}"/> interface extends the capabilities of 
/// <see cref="IReadResult{TInterface}"/> by providing methods to transition a read-only query result 
/// into mutable command objects for update or delete operations.
/// </para>
/// <para>
/// This interface plays a key role in the command-based data access pattern by:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Providing read-only access to items retrieved through query operations
///     </description>
///   </item>
///   <item>
///     <description>
///       Offering validation capabilities to verify the current state of an item
///     </description>
///   </item>
///   <item>
///     <description>
///       Enabling the transition from read operations to modification operations
///     </description>
///   </item>
/// </list>
/// <para>
/// This interface is returned by methods that execute query operations, such as:
/// </para>
/// <code>
/// var results = await queryCommand.ToAsyncEnumerable().ToArrayAsync();
/// foreach (var queryResult in results)
/// {
///     // Access item data in read-only mode
///     var id = queryResult.Item.Id;
///     
///     // Transition to update mode if needed
///     var updateCommand = queryResult.Update();
///     updateCommand.Item.SomeProperty = "new value";
///     await updateCommand.SaveAsync(requestContext, CancellationToken.None);
/// }
/// </code>
/// <para>
/// The interface enforces a state transition pattern where a query result can only be transitioned
/// to an update or delete command once. After calling either <see cref="Update"/> or <see cref="Delete"/>,
/// the query result becomes invalid and cannot be transitioned again.
/// </para>
/// </remarks>
/// <seealso cref="IReadResult{TInterface}"/>
/// <seealso cref="ISaveCommand{TInterface}"/>
/// <seealso cref="IQueryCommand{TInterface}"/>
public interface IQueryResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item retrieved from the query operation with read-only access.
    /// </summary>
    /// <value>
    /// A strongly-typed instance of <typeparamref name="TInterface"/> representing the retrieved item.
    /// All property access is read-only and any attempt to modify properties will throw an exception.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides access to the underlying item that was retrieved from the query operation.
    /// The item is wrapped in a dynamic proxy that intercepts all method calls, enforcing read-only
    /// access to maintain data integrity until a state transition occurs.
    /// </para>
    /// <para>
    /// Attempting to modify any property will result in an <see cref="InvalidOperationException"/> with
    /// a message indicating that the item is read-only. To modify the item, first call <see cref="Update"/>
    /// to transition to a mutable save command.
    /// </para>
    /// <para>
    /// This property functions identically to <see cref="IReadResult{TInterface}.Item"/>, providing
    /// consistent read-only access across all read operations in the system.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an attempt is made to modify any property of the item.
    /// </exception>
    TInterface Item { get; }

    /// <summary>
    /// Transitions this query result to a delete command that will mark the item as deleted when executed.
    /// </summary>
    /// <returns>
    /// An <see cref="ISaveCommand{TInterface}"/> configured for a delete operation,
    /// which can be executed to mark the item as deleted in the data store.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method creates a state transition from a read-only query result to a command
    /// that will mark the item as deleted when executed. The transition follows these steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Verifies this query result hasn't already been transitioned</description>
    ///   </item>
    ///   <item>
    ///     <description>Creates a delete command configured with the current item</description>
    ///   </item>
    ///   <item>
    ///     <description>Invalidates this query result to prevent further transitions</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The returned delete command is read-only (preventing further modifications to the item),
    /// but can be executed via its <see cref="ISaveCommand{TInterface}.SaveAsync"/> method to
    /// persist the deletion to the data store.
    /// </para>
    /// <para>
    /// After calling this method, this query result instance is invalidated and cannot be used
    /// for further operations. Attempting to call <see cref="Delete"/> or <see cref="Update"/>
    /// again will throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Deletion in this system follows a soft-delete pattern, where items are marked as deleted
    /// rather than physically removed from the data store.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <see cref="Delete"/> or <see cref="Update"/> has already been called on this instance,
    /// indicating the query result has already been transitioned and is no longer valid.
    /// </exception>
    /// <seealso cref="Update"/>
    /// <seealso cref="ISaveCommand{TInterface}"/>
    ISaveCommand<TInterface> Delete();

    /// <summary>
    /// Transitions this query result to an update command that allows modifying and saving the item.
    /// </summary>
    /// <returns>
    /// An <see cref="ISaveCommand{TInterface}"/> configured for an update operation,
    /// which can be used to modify the item and save changes to the data store.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method creates a state transition from a read-only query result to a command
    /// that allows modifying the item. The transition follows these steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Verifies this query result hasn't already been transitioned</description>
    ///   </item>
    ///   <item>
    ///     <description>Creates an update command configured with the current item</description>
    ///   </item>
    ///   <item>
    ///     <description>Invalidates this query result to prevent further transitions</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The returned update command allows modifications to the item's properties and can be
    /// executed via its <see cref="ISaveCommand{TInterface}.SaveAsync"/> method to persist
    /// the changes to the data store.
    /// </para>
    /// <para>
    /// After calling this method, this query result instance is invalidated and cannot be used
    /// for further operations. Attempting to call <see cref="Update"/> or <see cref="Delete"/>
    /// again will throw an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// // Retrieve an item through a query
    /// var queryResult = (await queryCommand.ToAsyncEnumerable().ToArrayAsync())[0];
    /// 
    /// // Transition to update mode
    /// var updateCommand = queryResult.Update();
    /// 
    /// // Modify properties
    /// updateCommand.Item.SomeProperty = "new value";
    /// 
    /// // Save changes
    /// await updateCommand.SaveAsync(requestContext, cancellationToken);
    /// </code>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <see cref="Update"/> or <see cref="Delete"/> has already been called on this instance,
    /// indicating the query result has already been transitioned and is no longer valid.
    /// </exception>
    /// <seealso cref="Delete"/>
    /// <seealso cref="ISaveCommand{TInterface}"/>
    ISaveCommand<TInterface> Update();

    /// <summary>
    /// Validates the current state of the retrieved item against configured validation rules.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous validation operation. The task result contains
    /// a <see cref="ValidationResult"/> indicating whether the item is valid and listing
    /// any validation errors that were found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method executes both system-defined and domain-specific validation rules against
    /// the item without modifying it. Validation is an important step before transitioning
    /// to an update or delete command to ensure the item is in a valid state.
    /// </para>
    /// <para>
    /// The validation is performed asynchronously to accommodate validation rules that might
    /// require external resource access or time-consuming operations.
    /// </para>
    /// <para>
    /// This method functions identically to <see cref="IReadResult{TInterface}.ValidateAsync"/>,
    /// providing consistent validation capabilities across all read operations in the system.
    /// </para>
    /// <para>
    /// Unlike the transition methods (<see cref="Update"/> and <see cref="Delete"/>), this method
    /// does not invalidate the query result, allowing validation to be performed multiple times
    /// before deciding whether to transition to a command.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <seealso cref="FluentValidation.Results.ValidationResult"/>
    /// <seealso cref="IReadResult{TInterface}.ValidateAsync"/>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements the <see cref="IQueryResult{TInterface}"/> interface, providing query result capabilities
/// with state transition to update and delete commands.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// This class extends <see cref="ProxyManager{TInterface, TItem}"/> to provide a concrete implementation
/// of <see cref="IQueryResult{TInterface}"/>. It manages read-only access to items retrieved from
/// query operations and provides methods to transition from read-only query results to mutable 
/// save commands for updates or deletions.
/// </para>
/// <para>
/// Key features of this implementation include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Read-only access to the underlying item through a dynamic proxy
///     </description>
///   </item>
///   <item>
///     <description>
///       Thread-safe state transitions using a semaphore to prevent race conditions
///     </description>
///   </item>
///   <item>
///     <description>
///       One-time conversion to update or delete commands with state invalidation to prevent reuse
///     </description>
///   </item>
///   <item>
///     <description>
///       Validation capabilities inherited from the proxy manager
///     </description>
///   </item>
/// </list>
/// <para>
/// Instances of this class are created by the static <see cref="Create"/> factory method,
/// which configures the dynamic proxy system and initializes all required dependencies.
/// </para>
/// <para>
/// This class is part of the query execution subsystem and is typically instantiated by
/// <see cref="IQueryCommand{TInterface}"/> implementations when materializing query results.
/// </para>
/// </remarks>
/// <seealso cref="IQueryResult{TInterface}"/>
/// <seealso cref="ProxyManager{TInterface, TItem}"/>
/// <seealso cref="IQueryCommand{TInterface}"/>
internal class QueryResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IQueryResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// The factory method to create a delete command for the item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This delegate is provided during initialization and encapsulates the logic for creating
    /// a properly configured delete command for the specific item type. When invoked, it:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Sets the item's <see cref="BaseItem.DeletedDate"/> to the current time</description>
    ///   </item>
    ///   <item>
    ///     <description>Sets the item's <see cref="BaseItem.IsDeleted"/> flag to <see langword="true"/></description>
    ///   </item>
    ///   <item>
    ///     <description>Creates a read-only save command to persist these changes</description>
    ///   </item>
    /// </list>
    /// <para>
    /// This field is set to <see langword="null"/> after the first transition to invalidate the
    /// query result and prevent reuse.
    /// </para>
    /// </remarks>
    private Func<TItem, ISaveCommand<TInterface>> _createDeleteCommand = null!;

    /// <summary>
    /// The factory method to create an update command for the item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This delegate is provided during initialization and encapsulates the logic for creating
    /// a properly configured update command for the specific item type. When invoked, it:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Updates the item's <see cref="BaseItem.UpdatedDate"/> to the current time</description>
    ///   </item>
    ///   <item>
    ///     <description>Creates a mutable save command that allows property modifications</description>
    ///   </item>
    /// </list>
    /// <para>
    /// This field is set to <see langword="null"/> after the first transition to invalidate the
    /// query result and prevent reuse.
    /// </para>
    /// </remarks>
    private Func<TItem, ISaveCommand<TInterface>> _createUpdateCommand = null!;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="QueryResult{TInterface, TItem}"/> instance that wraps the specified item
    /// with read-only access and state transition capabilities.
    /// </summary>
    /// <param name="item">The concrete item instance to wrap in a read-only proxy.</param>
    /// <param name="validateAsyncDelegate">The delegate used to validate the item against business rules.</param>
    /// <param name="createDeleteCommand">The factory method to create a properly configured delete command for the item.</param>
    /// <param name="createUpdateCommand">The factory method to create a properly configured update command for the item.</param>
    /// <returns>A fully configured <see cref="QueryResult{TInterface, TItem}"/> instance that implements <see cref="IQueryResult{TInterface}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This factory method creates and configures a <see cref="QueryResult{TInterface, TItem}"/> instance
    /// with all necessary components for proper operation. It follows these steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Creates a new <see cref="QueryResult{TInterface, TItem}"/> instance</description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Sets up the instance with the item reference, validation delegate, and command factory methods
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Configures the instance as read-only to prevent direct modifications to the item
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Creates a dynamic proxy that intercepts all method invocations on the item
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Links the proxy and the proxy manager for proper method interception
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The factory method pattern is used here to ensure proper initialization of the proxy system,
    /// which requires circular references between the proxy and its manager. This approach also
    /// encapsulates the complex initialization logic, providing a clean API for creating query results.
    /// </para>
    /// <para>
    /// This method is designed to be called by the <see cref="IQueryCommand{TInterface}"/> implementation
    /// when materializing query results, not directly by end users of the API.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the parameters is <see langword="null"/>.
    /// </exception>
    /// <seealso cref="IQueryResult{TInterface}"/>
    /// <seealso cref="IQueryCommand{TInterface}"/>
    public static QueryResult<TInterface, TItem> Create(
        TItem item,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate,
        Func<TItem, ISaveCommand<TInterface>> createDeleteCommand,
        Func<TItem, ISaveCommand<TInterface>> createUpdateCommand)
    {
        // Create the proxy manager - need an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new QueryResult<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = true,
            _validateAsyncDelegate = validateAsyncDelegate,
            _createDeleteCommand = createDeleteCommand,
            _createUpdateCommand = createUpdateCommand,
        };

        // Create the proxy
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // Set our proxy
        proxyManager._proxy = proxy;

        // Return the proxy manager
        return proxyManager;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public ISaveCommand<TInterface> Delete()
    {
        // Ensure that only one operation that modifies the item is in progress at a time
        _semaphore.Wait();

        try
        {
            // Check if already converted
            if (_createDeleteCommand is null)
            {
                throw new InvalidOperationException("The Delete() method cannot be called because either the Delete() or Update() method has already been called.");
            }

            var deleteCommand = _createDeleteCommand(_item);

            // Null out the convert delegates so we know that we have already converted and are no longer valid
            _createDeleteCommand = null!;
            _createUpdateCommand = null!;

            return deleteCommand;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public ISaveCommand<TInterface> Update()
    {
        // Ensure that only one operation that modifies the item is in progress at a time
        _semaphore.Wait();

        try
        {
            // Check if already converted
            if (_createUpdateCommand is null)
            {
                throw new InvalidOperationException("The Update() method cannot be called because either the Delete() or Update() method has already been called.");
            }

            var updateCommand = _createUpdateCommand(_item);

            // Null out the convert delegates so we know that we have already converted and are no longer valid
            _createDeleteCommand = null!;
            _createUpdateCommand = null!;

            return updateCommand;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion
}
