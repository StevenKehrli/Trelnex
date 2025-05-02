using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Interface to expose and validate an item read from the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// This interface provides access to the underlying item and validation capabilities,
/// allowing consumers to work with data in a consistent manner.
/// </remarks>
public interface IReadResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item retrieved from the data store.
    /// </summary>
    /// <value>The strongly-typed item interface.</value>
    TInterface Item { get; }

    /// <summary>
    /// Validates the item asynchronously using the configured validation rules.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous operation that returns a <see cref="ValidationResult"/>
    /// containing the outcome of the validation.
    /// </returns>
    /// <exception cref="System.OperationCanceledException">
    /// Thrown when the operation is canceled through the <paramref name="cancellationToken"/>.
    /// </exception>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements the read operation logic for retrieving and validating items from the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type representing the items in the data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items.</typeparam>
/// <remarks>
/// This class manages the proxy creation and validation process for items being read from storage.
/// It enforces read-only access to ensure data integrity during read operations.
/// </remarks>
internal class ReadResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IReadResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Public Methods

    /// <summary>
    /// Creates a new instance of <see cref="ReadResult{TInterface, TItem}"/> that wraps the provided item.
    /// </summary>
    /// <param name="item">The concrete item to be proxied.</param>
    /// <param name="validateAsyncDelegate">The delegate used to validate the item.</param>
    /// <returns>A configured <see cref="ReadResult{TInterface, TItem}"/> instance.</returns>
    /// <remarks>
    /// This factory method handles the creation of both the proxy manager and the proxy itself,
    /// ensuring they are correctly linked for proper operation.
    /// </remarks>
    public static ReadResult<TInterface, TItem> Create(
        TItem item,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate)
    {
        // Create the proxy manager - need an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new ReadResult<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = true,
            _validateAsyncDelegate = validateAsyncDelegate,
        };

        // Create the proxy that will be exposed to consumers
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // Set our proxy reference
        proxyManager._proxy = proxy;

        // Return the configured proxy manager
        return proxyManager;
    }

    #endregion
}
