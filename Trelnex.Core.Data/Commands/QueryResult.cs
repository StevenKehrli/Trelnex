using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Operations for accessing and transitioning query results.
/// </summary>
/// <typeparam name="TInterface">Interface type for items.</typeparam>
/// <remarks>
/// Extends <see cref="IReadResult{TInterface}"/> with methods to transition to mutable commands.
/// </remarks>
public interface IQueryResult<TInterface>
    : IDisposable
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Retrieved item with read-only access.
    /// </summary>
    TInterface Item { get; }

    /// <summary>
    /// Transitions to a delete command.
    /// </summary>
    /// <returns>Command for deleting the item.</returns>
    /// <exception cref="InvalidOperationException">
    /// When <see cref="Delete"/> or <see cref="Update"/> was already called.
    /// </exception>
    ISaveCommand<TInterface> Delete();

    /// <summary>
    /// Transitions to an update command.
    /// </summary>
    /// <returns>Command for updating the item.</returns>
    /// <exception cref="InvalidOperationException">
    /// When <see cref="Update"/> or <see cref="Delete"/> was already called.
    /// </exception>
    ISaveCommand<TInterface> Update();

    /// <summary>
    /// Validates item state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    /// <exception cref="OperationCanceledException">
    /// When operation is canceled.
    /// </exception>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements query result with state transition capabilities.
/// </summary>
/// <typeparam name="TInterface">Interface type for items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Concrete implementation of <see cref="IQueryResult{TInterface}"/>.
/// </remarks>
internal class QueryResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IQueryResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// Factory method for creating delete commands.
    /// </summary>
    private Func<TItem, ISaveCommand<TInterface>> _createDeleteCommand = null!;

    /// <summary>
    /// Factory method for creating update commands.
    /// </summary>
    private Func<TItem, ISaveCommand<TInterface>> _createUpdateCommand = null!;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates query result wrapping an item.
    /// </summary>
    /// <param name="item">Item to wrap in proxy.</param>
    /// <param name="validateAsyncDelegate">Validation delegate.</param>
    /// <param name="createDeleteCommand">Delete command factory.</param>
    /// <param name="createUpdateCommand">Update command factory.</param>
    /// <returns>Configured query result instance.</returns>
    /// <exception cref="ArgumentNullException">When any parameter is null.</exception>
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
        try
        {
            // Ensure that only one operation that modifies the item is in progress at a time
            _semaphore.Wait();

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
        try
        {
            // Ensure that only one operation that modifies the item is in progress at a time
            _semaphore.Wait();

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
