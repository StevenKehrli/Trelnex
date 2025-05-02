using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents the result of a save operation for an item of type <typeparamref name="TItem"/>.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for the saved item. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type that was saved. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="HttpStatusCode">The HTTP status code representing the outcome of the save operation.</param>
/// <param name="Item">The saved item, or <see langword="null"/> if the operation did not result in a valid item.</param>
/// <remarks>
/// This record is typically returned by repository or service methods that perform save operations.
/// The HTTP status code provides context about the operation result:
/// - 200 (OK): Indicates a successful update
/// - 201 (Created): Indicates a successful creation
/// - 4xx/5xx: Indicates various error conditions
/// </remarks>
public record SaveResult<TInterface, TItem>(
    HttpStatusCode HttpStatusCode,
    TItem? Item)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
