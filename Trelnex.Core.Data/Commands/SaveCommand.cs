using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// The interface to validate and save the item in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// This interface provides methods for validating an item before saving it to a data store,
/// as well as accessing the item being operated on.
/// </remarks>
public interface ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item being operated on by this command.
    /// </summary>
    /// <value>The item instance that will be validated and saved.</value>
    TInterface Item { get; }

    /// <summary>
    /// Saves the item to the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"/> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the saved item wrapped in a <see cref="IReadResult{TInterface}"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the command is no longer valid (e.g., already saved).</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    Task<IReadResult<TInterface>> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the item as an asynchronous operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the validation result.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of <see cref="ISaveCommand{TInterface}"/> that handles saving items to a backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete type of the items in the backing data store.</typeparam>
/// <remarks>
/// This class manages the lifecycle of saving an item, including validation, change tracking,
/// and interaction with the underlying data store.
/// </remarks>
internal class SaveCommand<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// The type of save action being performed.
    /// </summary>
    private SaveAction _saveAction;

    /// <summary>
    /// The delegate responsible for performing the actual save operation.
    /// </summary>
    /// <remarks>
    /// This field is set to <see langword="null"/> after a save operation completes
    /// to indicate the command has been used and is no longer valid.
    /// </remarks>
    private SaveAsyncDelegate<TInterface, TItem> _saveAsyncDelegate = null!;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a proxy item over an existing item.
    /// </summary>
    /// <param name="item">The item to wrap.</param>
    /// <param name="isReadOnly">Indicates if the item is read-only.</param>
    /// <param name="validateAsyncDelegate">The delegate to validate the item.</param>
    /// <param name="saveAction">The type of save action.</param>
    /// <param name="saveAsyncDelegate">The delegate to save the item.</param>
    /// <returns>A new <see cref="SaveCommand{TInterface, TItem}"/> instance.</returns>
    /// <remarks>
    /// This factory method creates a proxy around the item to track changes and control access.
    /// </remarks>
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
        // ensure that only one operation that modifies the item is in progress at a time
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
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
        // ensure that only one operation that modifies the item is in progress at a time
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
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
