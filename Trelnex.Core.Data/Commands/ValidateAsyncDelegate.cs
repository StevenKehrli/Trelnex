using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for validating items asynchronously.
/// </summary>
/// <typeparam name="TInterface">Interface type for the item.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <param name="item">Item to validate.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Validation result.</returns>
/// <remarks>
/// Abstracts validation to support flexible implementations.
/// </remarks>
internal delegate Task<ValidationResult> ValidateAsyncDelegate<TInterface, TItem>(
    TItem item,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
