using FluentValidation.Results;

namespace Trelnex.Core.Data;

internal delegate Task<ValidationResult> ValidateAsyncDelegate<TInterface, TItem>(
    TItem item,
    CancellationToken cancellationToken)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface;
