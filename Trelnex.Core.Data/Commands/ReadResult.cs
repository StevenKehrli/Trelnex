using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// The class to expose and validate a item read from the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface IReadResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// The item.
    /// </summary>
    TInterface Item { get; }

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
internal class ReadResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IReadResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// Create a proxy item over a item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="validateAsyncDelegate">The action to validate the item.</param>
    /// <returns>A proxy item as TInterface.</returns>
    public static ReadResult<TInterface, TItem> Create(
        TItem item,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate)
    {
        // create the proxy manager - need an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new ReadResult<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = true,
            _validateAsyncDelegate = validateAsyncDelegate,
        };

        // create the proxy
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // set our proxy
        proxyManager._proxy = proxy;

        // return the proxy manager
        return proxyManager;
    }
}
