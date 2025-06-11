using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Command for validating and persisting item changes.
/// </summary>
/// <typeparam name="TInterface">Interface type of the items.</typeparam>
/// <remarks>
/// Command pattern implementation for create, update, or delete operations.
/// </remarks>
public interface ISaveCommand<TInterface>
    : IDisposable
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Item being operated on.
    /// </summary>
    TInterface Item { get; }

    /// <summary>
    /// Persists the item.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read-only wrapper for the saved item.</returns>
    /// <exception cref="InvalidOperationException">When already executed.</exception>
    /// <exception cref="ValidationException">When validation fails.</exception>
    /// <exception cref="CommandException">When storage operation fails.</exception>
    /// <exception cref="OperationCanceledException">When canceled.</exception>
    Task<IReadResult<TInterface>> SaveAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the item without saving.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    /// <exception cref="OperationCanceledException">When canceled.</exception>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of the command pattern for item persistence.
/// </summary>
/// <typeparam name="TInterface">Interface type of the items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Extends <see cref="ProxyManager{TInterface, TItem}"/> to provide change tracking and access control.
/// </remarks>
internal class SaveCommand<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// Operation type being performed.
    /// </summary>
    private SaveAction _saveAction;

    /// <summary>
    /// Delegate for performing storage operations.
    /// </summary>
    private SaveAsyncDelegate<TInterface, TItem> _saveAsyncDelegate = null!;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a save command with change tracking and validation.
    /// </summary>
    /// <param name="item">Item to be wrapped.</param>
    /// <param name="isReadOnly">If true, item is read-only.</param>
    /// <param name="validateAsyncDelegate">Validation delegate.</param>
    /// <param name="saveAction">Operation type.</param>
    /// <param name="saveAsyncDelegate">Storage operation delegate.</param>
    /// <returns>Configured SaveCommand instance.</returns>
    /// <exception cref="ArgumentNullException">When required parameters are null.</exception>
    public static SaveCommand<TInterface, TItem> Create(
        TItem item,
        bool isReadOnly,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate,
        SaveAction saveAction,
        SaveAsyncDelegate<TInterface, TItem> saveAsyncDelegate)
    {
        // Create the proxy manager - needs an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new SaveCommand<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = isReadOnly,
            _validateAsyncDelegate = validateAsyncDelegate,
            _saveAction = saveAction,
            _saveAsyncDelegate = saveAsyncDelegate,
        };

        // Create the proxy using the proxy manager's OnInvoke method
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // Set the proxy on the proxy manager
        proxyManager._proxy = proxy;

        // Return the proxy manager
        return proxyManager;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task<IReadResult<TInterface>> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure that only one operation that modifies the item is in progress at a time
            await _semaphore.WaitAsync(cancellationToken);

            var request = CreateSaveRequest();

            // Validate the underlying item
            var validationResult = await ValidateAsync(cancellationToken);
            validationResult.ValidateOrThrow<TItem>();

            // Save the item using the provided delegate
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
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SaveRequest{TInterface, TItem}"/> to add to the batch.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command is no longer valid.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    /// <remarks>
    /// This method is designed for use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// </remarks>
    public async Task<SaveRequest<TInterface, TItem>> AcquireAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure that only one operation that modifies the item is in progress at a time
            await _semaphore.WaitAsync(cancellationToken);

            return CreateSaveRequest();
        }
        catch
        {
            // CreateSaveRequest may throw an exception if the command is no longer valid, so release the semaphore
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
    /// </remarks>
    internal IReadResult<TInterface> Update(
        TItem item)
    {
        // Set the updated item and proxy
        _item = item;
        _proxy = ItemProxy<TInterface, TItem>.Create(OnInvoke);
        _isReadOnly = true;

        // Null out the saveAsyncDelegate to indicate that the command is no longer valid
        _saveAsyncDelegate = null!;

        // Create the read result and return
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
    /// <returns>A <see cref="SaveRequest{TInterface, TItem}"/> representing the save request.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the command is no longer valid.
    /// </exception>
    private SaveRequest<TInterface, TItem> CreateSaveRequest()
    {
        // Check if already saved
        if (_saveAsyncDelegate is null)
        {
            throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
        }

        // Create the event to record the operation
        var itemEvent = ItemEvent<TItem>.Create(
            related: _item,
            saveAction: _saveAction,
            changes: GetPropertyChanges());

        return new SaveRequest<TInterface, TItem>(
            Item: _item,
            Event: itemEvent,
            SaveAction: _saveAction);
    }

    #endregion
}
