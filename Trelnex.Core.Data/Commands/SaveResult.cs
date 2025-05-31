using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Result of a save operation.
/// </summary>
/// <typeparam name="TInterface">Interface type for the item.</typeparam>
/// <typeparam name="TItem">Concrete item type.</typeparam>
/// <param name="HttpStatusCode">HTTP status code indicating operation outcome.</param>
/// <param name="Item">The saved item, or null if operation failed.</param>
/// <remarks>
/// Returned by repository or service methods.
/// </remarks>
public record SaveResult<TInterface, TItem>(
    HttpStatusCode HttpStatusCode,
    TItem? Item)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
