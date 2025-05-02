using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents an asynchronous validation delegate for validating items of type <typeparamref name="TItem"/>
/// that implement the <typeparamref name="TInterface"/> interface.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the contract for items to be validated. Must implement <see cref="IBaseItem"/>.</typeparam>
/// <typeparam name="TItem">The concrete item type to validate. Must inherit from <see cref="BaseItem"/> and implement <typeparamref name="TInterface"/>.</typeparam>
/// <param name="item">The item to validate.</param>
/// <param name="cancellationToken">A token to observe for cancellation requests during validation.</param>
/// <returns>A task representing the asynchronous validation operation that resolves to a <see cref="ValidationResult"/> containing validation outcomes.</returns>
/// <remarks>
/// This delegate is used internally by validation handlers to perform asynchronous validation of domain objects.
/// It abstracts the validation process, allowing for flexible implementation of validation logic.
/// </remarks>
internal delegate Task<ValidationResult> ValidateAsyncDelegate<TInterface, TItem>(
    TItem item,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
