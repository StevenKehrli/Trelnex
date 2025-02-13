using FluentValidation;

namespace Trelnex.Core.Validation;

public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, DateTime> NotDefault<T>(
        this IRuleBuilder<T, DateTime> validator)
    {
        return validator.Must(k => k != default);
    }

    public static IRuleBuilderOptions<T, Guid> NotDefault<T>(
        this IRuleBuilder<T, Guid> validator)
    {
        return validator.Must(k => k != default);
    }
}
