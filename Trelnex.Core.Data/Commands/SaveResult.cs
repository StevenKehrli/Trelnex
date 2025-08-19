using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents the result of a save operation with status and item data.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="HttpStatusCode">HTTP status code indicating the outcome of the save operation.</param>
/// <param name="Item">The saved item if successful, or null if the operation failed.</param>
public record SaveResult<TItem>(
    HttpStatusCode HttpStatusCode,
    TItem? Item)
    where TItem : BaseItem;
