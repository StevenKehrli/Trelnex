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
/// <para>
/// This delegate is used internally by <see cref="SaveCommand{TInterface, TItem}"/> and other validation handlers 
/// to perform asynchronous validation of domain objects. It abstracts the validation process, allowing for flexible 
/// implementation of validation logic that can be swapped out or customized as needed.
/// </para>
/// <para>
/// The validation is typically performed using FluentValidation validators, but the delegate pattern allows for any 
/// validation approach to be used, making it extensible and decoupled from specific validation frameworks.
/// </para>
/// <para>
/// When the validation is successful, the <see cref="ValidationResult.IsValid"/> property will be <see langword="true"/>.
/// When validation fails, the result will contain a collection of validation errors with details about what failed
/// and why, allowing for rich error reporting to clients.
/// </para>
/// <para>
/// This delegate is typically invoked as part of a command's execution pipeline before attempting to save
/// changes to the data store, ensuring that only valid data is persisted.
/// </para>
/// </remarks>
/// <seealso cref="SaveCommand{TInterface, TItem}"/>
/// <seealso cref="SaveRequest{TInterface, TItem}"/>
/// <seealso cref="ValidationResult"/>
internal delegate Task<ValidationResult> ValidateAsyncDelegate<TInterface, TItem>(
    TItem item,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
