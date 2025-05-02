using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines a command for validating and persisting changes to an item in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="ISaveCommand{TInterface}"/> interface represents a command that encapsulates an operation
/// to create, update, or delete an item in the data store. It follows the Command pattern, separating
/// the request for an operation from its execution.
/// </para>
/// <para>
/// Key characteristics of this interface include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Access to the item being operated on through the <see cref="Item"/> property,
///       which may be read-only or mutable depending on the operation type
///     </description>
///   </item>
///   <item>
///     <description>
///       Pre-execution validation via the <see cref="ValidateAsync"/> method to verify the item's state
///     </description>
///   </item>
///   <item>
///     <description>
///       Execution of the operation via the <see cref="SaveAsync"/> method to persist changes
///     </description>
///   </item>
///   <item>
///     <description>
///       One-time execution semantics, where commands become invalid after being executed
///     </description>
///   </item>
/// </list>
/// <para>
/// Save commands are created by command providers for specific operations:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <c>Create</c> commands allow full modification of a new item's properties
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>Update</c> commands allow modification of an existing item's properties
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>Delete</c> commands mark an item as deleted but do not allow property modifications
///     </description>
///   </item>
/// </list>
/// <para>
/// Save commands can also be executed as part of a batch operation through <see cref="IBatchCommand{TInterface}"/>,
/// which ensures atomic execution of multiple commands.
/// </para>
/// <para>
/// Typical usage pattern:
/// </para>
/// <code>
/// // Create a new item
/// var createCommand = commandProvider.Create(id, partitionKey);
/// createCommand.Item.PropertyA = "Value A";
/// createCommand.Item.PropertyB = 123;
///
/// // Validate before saving (optional)
/// var validationResult = await createCommand.ValidateAsync(cancellationToken);
/// if (validationResult.IsValid)
/// {
///     // Save the item
///     var result = await createCommand.SaveAsync(requestContext, cancellationToken);
/// }
/// </code>
/// </remarks>
/// <seealso cref="IReadResult{TInterface}"/>
/// <seealso cref="IBatchCommand{TInterface}"/>
/// <seealso cref="ICommandProvider{TInterface}"/>
public interface ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item being operated on by this command with access appropriate to the operation type.
    /// </summary>
    /// <value>
    /// A strongly-typed instance of <typeparamref name="TInterface"/> representing the item being created,
    /// updated, or deleted. Access may be read-only or mutable depending on the operation type.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides access to the underlying item being operated on by the command.
    /// The access level varies based on the command type:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       For <c>Create</c> and <c>Update</c> commands, the property allows full read-write access,
    ///       enabling modification of the item's properties before saving.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       For <c>Delete</c> commands, the property provides read-only access, preventing modifications
    ///       to an item that is about to be deleted.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// The item is wrapped in a dynamic proxy that tracks changes to properties marked with the
    /// <see cref="TrackChangeAttribute"/>, enabling change tracking and validation.
    /// </para>
    /// <para>
    /// Property modifications are only allowed until the command is executed via <see cref="SaveAsync"/>.
    /// After execution, the command becomes invalid and the item transitions to read-only access.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify properties of an item in a read-only command (e.g., a delete command).
    /// </exception>
    TInterface Item { get; }

    /// <summary>
    /// Executes the command and persists the item to the backing data store.
    /// </summary>
    /// <param name="requestContext">
    /// The <see cref="IRequestContext"/> that provides contextual information about the request,
    /// including user identity and request metadata for auditing and event tracking.
    /// </param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous save operation. The task result contains an
    /// <see cref="IReadResult{TInterface}"/> that wraps the saved item with read-only access.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method executes the command, persisting the encapsulated changes to the backing data store.
    /// The execution process follows these steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       Validates the item against all applicable business rules
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Creates an event that captures the operation details and property changes
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Persists both the item and its corresponding event to the data store
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Returns a read-only result containing the saved item
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// After execution, the command becomes invalid and cannot be executed again. This prevents
    /// accidental reuse of the same command, which could lead to duplicate operations.
    /// </para>
    /// <para>
    /// The method is thread-safe, using a semaphore to ensure that only one operation that modifies
    /// the item can be in progress at a time.
    /// </para>
    /// <para>
    /// Example usage:
    /// </para>
    /// <code>
    /// // Create and configure a command
    /// var command = commandProvider.Create(id, partitionKey);
    /// command.Item.Name = "Example Item";
    ///
    /// // Execute the command
    /// var result = await command.SaveAsync(
    ///     requestContext: new RequestContext { UserId = "user123" },
    ///     cancellationToken: CancellationToken.None);
    ///
    /// // Access the saved item through the result
    /// var savedItem = result.Item;
    /// </code>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command has already been executed and is no longer valid.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Thrown when the item fails validation against business rules.
    /// </exception>
    /// <exception cref="CommandException">
    /// Thrown when the data store operation fails, with HTTP status codes indicating the nature of the failure.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <seealso cref="ValidateAsync"/>
    /// <seealso cref="IReadResult{TInterface}"/>
    Task<IReadResult<TInterface>> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the current state of the item against configured validation rules without saving.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous validation operation. The task result contains a
    /// <see cref="ValidationResult"/> indicating whether the item is valid and listing any
    /// validation errors that were found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs pre-flight validation of the item without persisting any changes to the
    /// data store. It executes both system-defined and domain-specific validation rules to determine
    /// if the item is in a valid state for the planned operation.
    /// </para>
    /// <para>
    /// Validation rules typically include:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       Base validation rules for <see cref="IBaseItem"/> properties (Id, PartitionKey, TypeName, etc.)
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Domain-specific validation rules defined for the concrete item type
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This method is useful for checking if an operation would succeed before actually executing it,
    /// allowing for more granular error handling and user feedback:
    /// </para>
    /// <code>
    /// // Create and configure a command
    /// var command = commandProvider.Create(id, partitionKey);
    /// command.Item.Name = "Example Item";
    ///
    /// // Validate before saving
    /// var validationResult = await command.ValidateAsync(CancellationToken.None);
    /// if (!validationResult.IsValid)
    /// {
    ///     // Handle validation errors
    ///     foreach (var error in validationResult.Errors)
    ///     {
    ///         Console.WriteLine($"Validation error: {error.ErrorMessage}");
    ///     }
    ///     return;
    /// }
    ///
    /// // Proceed with saving if validation succeeds
    /// var result = await command.SaveAsync(requestContext, CancellationToken.None);
    /// </code>
    /// <para>
    /// Unlike <see cref="SaveAsync"/>, this method does not modify the command's state, allowing
    /// multiple validation calls before deciding whether to proceed with the operation.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <seealso cref="FluentValidation.Results.ValidationResult"/>
    /// <seealso cref="SaveAsync"/>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements the command pattern for creating, updating, and deleting items in the data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// This class provides a concrete implementation of <see cref="ISaveCommand{TInterface}"/> that
/// encapsulates the process of saving changes to an item. It extends <see cref="ProxyManager{TInterface, TItem}"/>
/// to leverage the dynamic proxy system for change tracking and access control.
/// </para>
/// <para>
/// Key features of this implementation include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Property change tracking for auditing and event generation
///     </description>
///   </item>
///   <item>
///     <description>
///       Access control based on operation type (read-write for create/update, read-only for delete)
///     </description>
///   </item>
///   <item>
///     <description>
///       Thread safety through semaphore-based concurrency control
///     </description>
///   </item>
///   <item>
///     <description>
///       One-time execution semantics with state invalidation after use
///     </description>
///   </item>
///   <item>
///     <description>
///       Validation before persistence to ensure data integrity
///     </description>
///   </item>
/// </list>
/// <para>
/// The class uses a factory method pattern through the static <see cref="Create"/> method to ensure
/// proper initialization of the complex proxy system. This approach encapsulates the circular references
/// needed between the proxy and its manager.
/// </para>
/// <para>
/// The implementation follows a consistent pattern for all operation types (create, update, delete):
/// </para>
/// <list type="number">
///   <item>
///     <description>Initialize with appropriate configuration (read-only status, save action)</description>
///   </item>
///   <item>
///     <description>Allow property modifications if appropriate for the operation type</description>
///   </item>
///   <item>
///     <description>Validate the item state before persistence</description>
///   </item>
///   <item>
///     <description>Generate an event capturing the operation details and property changes</description>
///   </item>
///   <item>
///     <description>Persist both the item and its event to the data store</description>
///   </item>
///   <item>
///     <description>Invalidate the command to prevent reuse</description>
///   </item>
/// </list>
/// <para>
/// This class also implements special handling for batch operations through its <see cref="AcquireAsync"/>
/// and <see cref="Update"/> methods, which are used by <see cref="BatchCommand{TInterface, TItem}"/>
/// to coordinate atomic multi-item operations.
/// </para>
/// </remarks>
/// <seealso cref="ISaveCommand{TInterface}"/>
/// <seealso cref="ProxyManager{TInterface, TItem}"/>
/// <seealso cref="IBatchCommand{TInterface}"/>
internal class SaveCommand<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// The type of save action being performed (Create, Update, or Delete).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This field stores the operation type for the command, which determines:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>The type of event generated for auditing</description>
    ///   </item>
    ///   <item>
    ///     <description>Special handling for different operation types</description>
    ///   </item>
    ///   <item>
    ///     <description>User-facing messaging about the operation</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Possible values come from the <see cref="SaveAction"/> enum:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="SaveAction.CREATED"/> - Creating a new item</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="SaveAction.UPDATED"/> - Updating an existing item</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="SaveAction.DELETED"/> - Marking an item as deleted</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <seealso cref="SaveAction"/>
    /// <seealso cref="CreateSaveRequest"/>
    private SaveAction _saveAction;

    /// <summary>
    /// The delegate responsible for performing the actual save operation against the data store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This delegate encapsulates the storage-specific logic for persisting an item to the backing
    /// data store. It is provided during initialization and allows the command to be independent
    /// of the specific storage implementation.
    /// </para>
    /// <para>
    /// The delegate is invoked by the <see cref="SaveAsync"/> method and receives:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>A <see cref="SaveRequest{TInterface, TItem}"/> containing the item and its event</description>
    ///   </item>
    ///   <item>
    ///     <description>A cancellation token for monitoring cancellation requests</description>
    ///   </item>
    /// </list>
    /// <para>
    /// This field is set to <see langword="null"/> after a save operation completes, serving as a
    /// marker that the command has been used and is no longer valid for further operations. Subsequent
    /// attempts to use the command will result in an <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="SaveAsyncDelegate{TInterface, TItem}"/>
    /// <seealso cref="SaveAsync"/>
    private SaveAsyncDelegate<TInterface, TItem> _saveAsyncDelegate = null!;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new save command that wraps an item with change tracking and validation capabilities.
    /// </summary>
    /// <param name="item">The concrete item instance to be wrapped and operated on.</param>
    /// <param name="isReadOnly">
    /// Indicates if the item should be accessed in read-only mode (true) or allow modifications (false).
    /// Usually true for delete operations and false for create/update operations.
    /// </param>
    /// <param name="validateAsyncDelegate">The delegate used to validate the item against business rules.</param>
    /// <param name="saveAction">
    /// The type of operation being performed (Create, Update, or Delete) as defined in <see cref="SaveAction"/>.
    /// </param>
    /// <param name="saveAsyncDelegate">
    /// The delegate responsible for persisting the item to the backing data store.
    /// </param>
    /// <returns>
    /// A fully configured <see cref="SaveCommand{TInterface, TItem}"/> instance ready for use.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This factory method creates and configures a <see cref="SaveCommand{TInterface, TItem}"/> instance
    /// with all necessary components for the specific operation type (create, update, or delete).
    /// It follows these steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Creates a new command instance with the specified configuration</description>
    ///   </item>
    ///   <item>
    ///     <description>Creates a dynamic proxy to intercept property access and track changes</description>
    ///   </item>
    ///   <item>
    ///     <description>Links the proxy and command together for proper operation</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The factory method pattern is used here to handle the complex initialization requirements,
    /// particularly the circular reference between the proxy and its manager. This pattern also
    /// encapsulates the initialization logic, providing a clean API for command creation.
    /// </para>
    /// <para>
    /// The <paramref name="isReadOnly"/> parameter determines whether the wrapped item allows
    /// property modifications:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       For create and update operations, this should be <see langword="false"/> to allow
    ///       property modifications before saving.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       For delete operations, this should be <see langword="true"/> to prevent modifications
    ///       to an item that is about to be deleted.
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// This method is typically called by command providers when creating commands for specific
    /// operations, not directly by end users of the API.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if any of the parameters (except <paramref name="isReadOnly"/>) is <see langword="null"/>.
    /// </exception>
    /// <seealso cref="SaveAction"/>
    /// <seealso cref="ValidateAsyncDelegate{TInterface, TItem}"/>
    /// <seealso cref="SaveAsyncDelegate{TInterface, TItem}"/>
    public static SaveCommand<TInterface, TItem> Create(
        TItem item,
        bool isReadOnly,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate,
        SaveAction saveAction,
        SaveAsyncDelegate<TInterface, TItem> saveAsyncDelegate)
    {
        // create the proxy manager - need an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new SaveCommand<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = isReadOnly,
            _validateAsyncDelegate = validateAsyncDelegate,
            _saveAction = saveAction,
            _saveAsyncDelegate = saveAsyncDelegate,
        };

        // create the proxy
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // set our proxy
        proxyManager._proxy = proxy;

        // return the proxy manager
        return proxyManager;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task<IReadResult<TInterface>> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        try
        {
            // ensure that only one operation that modifies the item is in progress at a time
            await _semaphore.WaitAsync(cancellationToken);

            var request = CreateSaveRequest(requestContext);

            // validate the underlying item
            var validationResult = await ValidateAsync(cancellationToken);
            validationResult.ValidateOrThrow<TItem>();

            // save the item
            var item = await _saveAsyncDelegate(
                request,
                cancellationToken);

            return Update(item);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Acquires exclusive access to this command and its item.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"/> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SaveRequest{TInterface, TItem}"/> to add to the batch.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command is no longer valid because its <see cref="SaveAsync"/> method
    /// has already been called.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
    /// </exception>
    /// <remarks>
    /// This method is designed for use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// </remarks>
    public async Task<SaveRequest<TInterface, TItem>> AcquireAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ensure that only one operation that modifies the item is in progress at a time
            await _semaphore.WaitAsync(cancellationToken);

            return CreateSaveRequest(requestContext);
        }
        catch
        {
            // CreateSaveRequest may throw an exception if the command is no longer valid
            _semaphore.Release();

            throw;
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Updates this command with the result of a save operation.
    /// </summary>
    /// <param name="item">The item that was saved.</param>
    /// <returns>A <see cref="IReadResult{TInterface}"/> representing the saved item.</returns>
    /// <remarks>
    /// This method is designed for use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// It transitions the command to a read-only state after the save operation.
    /// </remarks>
    internal IReadResult<TInterface> Update(
        TItem item)
    {
        // set the updated item and proxy
        _item = item;
        _proxy = ItemProxy<TInterface, TItem>.Create(OnInvoke);
        _isReadOnly = true;

        // null out the saveAsyncDelegate so we know that we have already saved and are no longer valid
        _saveAsyncDelegate = null!;

        // create the read result and return
        return ReadResult<TInterface, TItem>.Create(
            item: item,
            validateAsyncDelegate: _validateAsyncDelegate);
    }

    /// <summary>
    /// Releases exclusive access to this command and its item.
    /// </summary>
    /// <remarks>
    /// This method is designed for use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// </remarks>
    internal void Release()
    {
        _semaphore.Release();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a save request for the current item state.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"/> that invoked this method.</param>
    /// <returns>A <see cref="SaveRequest{TInterface, TItem}"/> representing the save request.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command is no longer valid because its <see cref="SaveAsync"/> method
    /// has already been called.
    /// </exception>
    private SaveRequest<TInterface, TItem> CreateSaveRequest(
        IRequestContext requestContext)
    {
        // check if already saved
        if (_saveAsyncDelegate is null)
        {
            throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
        }

        // create the event
        var itemEvent = ItemEvent<TItem>.Create(
            related: _item,
            saveAction: _saveAction,
            changes: GetPropertyChanges(),
            requestContext: requestContext);

        return new SaveRequest<TInterface, TItem>(
            Item: _item,
            Event: itemEvent,
            SaveAction: _saveAction);
    }

    #endregion
}
