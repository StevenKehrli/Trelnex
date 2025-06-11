using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Read-only access to items.
/// </summary>
/// <typeparam name="TInterface">Interface type for items.</typeparam>
/// <remarks>
/// Standardized wrapper for retrieved items.
/// </remarks>
public interface IReadResult<TInterface>
    : IDisposable
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item with read-only access.
    /// </summary>
    TInterface Item { get; }

    /// <summary>
    /// Validates the item.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task containing a <see cref="ValidationResult"/>.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements read-only access and validation.
/// </summary>
/// <typeparam name="TInterface">Interface type for data store items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Concrete implementation of <see cref="IReadResult{TInterface}"/>.
/// </remarks>
internal class ReadResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IReadResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Public Methods

    /// <summary>
    /// Creates a read-only wrapper for the provided item.
    /// </summary>
    /// <param name="item">The concrete item to be wrapped.</param>
    /// <param name="validateAsyncDelegate">The validation delegate.</param>
    /// <returns>A configured ReadResult instance.</returns>
    /// <remarks>
    /// Factory method.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if item or validateAsyncDelegate is null.
    /// </exception>
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
