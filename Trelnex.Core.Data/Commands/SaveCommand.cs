using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// The interface to validate and save the item in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// The item.
    /// </summary>
    TInterface Item { get; }

    /// <summary>
    /// The action to save the item to the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The item that was saved.</returns>
    Task<IReadResult<TInterface>> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// The action to validate the item.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The fluent <see cref="ValidationResult"/> item that was saved.</returns>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// The class to save the item in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
internal class SaveCommand<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, ISaveCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// The type of save action.
    /// </summary>
    private SaveAction _saveAction;

    /// <summary>
    /// The action to save the item.
    /// </summary>
    private SaveAsyncDelegate<TInterface, TItem> _saveAsyncDelegate = null!;

    /// <summary>
    /// Create a proxy item over a item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="isReadOnly">Indicates if the item is read-only.</param>
    /// <param name="validateAsyncDelegate">The action to validate the item.</param>
    /// <param name="saveAction">The type of save action.</param>
    /// <param name="saveAsyncDelegate">The action to save the item.</param>
    /// <returns>A proxy item as TInterface.</returns>
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

    /// <summary>
    /// The action to save the item.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="IReadResult{TInterface}"/> representing the saved item.</returns>
    /// <exception cref="InvalidOperationException">The command is no longer valid.</exception>
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
    /// Acquire exclusive access to this command and its item. For use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="SaveRequest{TInterface, TItem}"/> to add to the batch.</returns>
    /// <exception cref="InvalidOperationException">The command is no longer valid.</exception>
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

    /// <summary>
    /// Update this command with the result of a batch save operation. For use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// </summary>
    /// <param name="item">The item that was saved.</param>
    /// <returns>A <see cref="IReadResult{TInterface}"/> representing the saved item.</returns>
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
    /// Release exclusive access to this command and its item. For use by <see cref="BatchCommand{TInterface, TItem}"/>.
    /// </summary>
    internal void Release()
    {
        _semaphore.Release();
    }

    /// <summary>
    /// Create a save request.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"> that invoked this method.</param>
    /// <returns>A <see cref="SaveRequest{TInterface, TItem}"/> representing the save request.</returns>
    /// <exception cref="InvalidOperationException">The command is no longer valid.</exception>
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
}
