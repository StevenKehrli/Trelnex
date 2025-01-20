using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// The class to expose and validate a item read from the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface IQueryResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// The item.
    /// </summary>
    TInterface Item { get; }

    /// <summary>
    /// Returns an <see cref="ISaveCommand{TInterface}"/> to delete the item.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> to delete the item.</returns>
    ISaveCommand<TInterface> Delete();

    /// <summary>
    /// Returns an <see cref="ISaveCommand{TInterface}"/> to update the item.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> to update the item.</returns>
    ISaveCommand<TInterface> Update();

    /// <summary>
    /// The action to validate the item.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The fluent <see cref="ValidationResult"/>item that was saved.</returns>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// The class to read the item in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
internal class QueryResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IQueryResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// The method to create a command to delete the item.
    /// </summary>
    private Func<TItem, ISaveCommand<TInterface>> _createDeleteCommand = null!;

    /// <summary>
    /// The method to create a command to update the item.
    /// </summary>
    private Func<TItem, ISaveCommand<TInterface>> _createUpdateCommand = null!;

    /// <summary>
    /// Create a proxy item over a item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="validateAsyncDelegate">The action to validate the item.</param>
    /// <param name="createDeleteCommand">The method to create a <see cref="ISaveCommand{TInterface}" to delete the item.</param>
    /// <param name="createUpdateCommand">The method to create a <see cref="ISaveCommand{TInterface}" to update the item.</param>
    /// <returns>A proxy item as TInterface.</returns>
    public static QueryResult<TInterface, TItem> Create(
        TItem item,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate,
        Func<TItem, ISaveCommand<TInterface>> createDeleteCommand,
        Func<TItem, ISaveCommand<TInterface>> createUpdateCommand)
    {
        // create the proxy manager - need an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new QueryResult<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = true,
            _validateAsyncDelegate = validateAsyncDelegate,
            _createDeleteCommand = createDeleteCommand,
            _createUpdateCommand = createUpdateCommand,
        };

        // create the proxy
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // set our proxy
        proxyManager._proxy = proxy;

        // return the proxy manager
        return proxyManager;
    }

    /// <summary>
    /// Returns an <see cref="ISaveCommand{TInterface}"/> to delete the item.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> to delete the item.</returns>
    public ISaveCommand<TInterface> Delete()
    {
        // ensure that only one operation that modifies the item is in progress at a time
        _semaphore.Wait();

        try
        {
            // check if already converted
            if (_createDeleteCommand is null)
            {
                throw new InvalidOperationException("The Delete() method cannot be called because either the Delete() or Update() method has already been called.");
            }

            var deleteCommand = _createDeleteCommand(_item);

            // null out the convert delegates so we know that we have already converted and are no longer valid
            _createDeleteCommand = null!;
            _createUpdateCommand = null!;

            return deleteCommand;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Returns an <see cref="ISaveCommand{TInterface}"/> to update the item.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> to update the item.</returns>
    public ISaveCommand<TInterface> Update()
    {
        // ensure that only one operation that modifies the item is in progress at a time
        _semaphore.Wait();

        try
        {
            // check if already converted
            if (_createUpdateCommand is null)
            {
                throw new InvalidOperationException("The Update() method cannot be called because either the Delete() or Update() method has already been called.");
            }

            var updateCommand = _createUpdateCommand(_item);

            // null out the convert delegates so we know that we have already converted and are no longer valid
            _createDeleteCommand = null!;
            _createUpdateCommand = null!;

            return updateCommand;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
