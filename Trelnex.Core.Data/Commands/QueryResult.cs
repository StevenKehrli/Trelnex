using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for exposing and validating an item read from the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// This interface provides methods to validate, update, or delete an item retrieved through a query operation.
/// </remarks>
public interface IQueryResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item retrieved from the data store.
    /// </summary>
    /// <value>The retrieved item as <typeparamref name="TInterface"/>.</value>
    TInterface Item { get; }

    /// <summary>
    /// Creates a command that will delete the item when executed.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> that can be used to delete the item.</returns>
    /// <remarks>
    /// After this method is called, no further operations can be performed on this query result.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <see cref="Delete"/> or <see cref="Update"/> has already been called on this instance.
    /// </exception>
    ISaveCommand<TInterface> Delete();

    /// <summary>
    /// Creates a command that will update the item when executed.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> that can be used to update the item.</returns>
    /// <remarks>
    /// After this method is called, no further operations can be performed on this query result.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if either <see cref="Delete"/> or <see cref="Update"/> has already been called on this instance.
    /// </exception>
    ISaveCommand<TInterface> Update();

    /// <summary>
    /// Validates the current state of the item.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the item is valid.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the <paramref name="cancellationToken"/>.
    /// </exception>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of <see cref="IQueryResult{TInterface}"/> that provides functionality to read and modify items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items in the backing data store.</typeparam>
/// <remarks>
/// This class manages proxying calls to the underlying item and ensures thread-safety when performing operations.
/// </remarks>
internal class QueryResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IQueryResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// The method to create a command to delete the item.
    /// </summary>
    private Func<TItem, ISaveCommand<TInterface>> _createDeleteCommand = null!;

    /// <summary>
    /// The method to create a command to update the item.
    /// </summary>
    private Func<TItem, ISaveCommand<TInterface>> _createUpdateCommand = null!;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new <see cref="QueryResult{TInterface, TItem}"/> instance that wraps the specified item.
    /// </summary>
    /// <param name="item">The item to wrap.</param>
    /// <param name="validateAsyncDelegate">The delegate used to validate the item.</param>
    /// <param name="createDeleteCommand">The method to create a command to delete the item.</param>
    /// <param name="createUpdateCommand">The method to create a command to update the item.</param>
    /// <returns>A new <see cref="QueryResult{TInterface, TItem}"/> instance.</returns>
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
