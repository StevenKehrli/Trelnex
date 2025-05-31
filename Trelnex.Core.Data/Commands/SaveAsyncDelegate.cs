namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for saving a single item.
/// </summary>
/// <typeparam name="TInterface">Interface type for the item.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <param name="request">Save request with item and metadata.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The saved item.</returns>
/// <remarks>
/// Abstracts storage implementation details.
/// </remarks>
internal delegate Task<TItem> SaveAsyncDelegate<TInterface, TItem>(
    SaveRequest<TInterface, TItem> request,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
