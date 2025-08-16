using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Delegate for asynchronously validating an item.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="item">The item to validate.</param>
/// <param name="cancellationToken">Token to cancel the validation operation.</param>
/// <returns>Validation result indicating success or failure with details.</returns>
internal delegate Task<ValidationResult> ValidateAsyncDelegate<TItem>(
    TItem item,
    CancellationToken cancellationToken)
    where TItem : BaseItem;
