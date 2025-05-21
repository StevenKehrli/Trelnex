using FluentValidation;

namespace Trelnex.Core.Validation;

/// <summary>
/// Extension methods for FluentValidation rule builders.
/// </summary>
/// <remarks>
/// Provides additional validation rules that can be used with FluentValidation
/// to simplify common validation scenarios and extend the built-in validation capabilities.
/// These extensions follow the FluentValidation fluent API pattern.
/// </remarks>
public static class ValidatorExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Validates that a DateTime property is not the default value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The rule builder being extended.</param>
    /// <returns>Options builder for configuring the validation rule.</returns>
    /// <remarks>
    /// Ensures that a DateTime field has been explicitly set.
    /// </remarks>
    /// <example>
    /// <code>
    /// RuleFor(x => x.CreatedDate).NotDefault();
    /// </code>
    /// </example>
    public static IRuleBuilderOptions<T, DateTime> NotDefault<T>(
        this IRuleBuilder<T, DateTime> validator)
    {
        // Use Must to define the validation logic.
        return validator.Must(k => k != default);
    }

    /// <summary>
    /// Validates that a DateTimeOffset property is not the default value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The rule builder being extended.</param>
    /// <returns>Options builder for configuring the validation rule.</returns>
    /// <remarks>
    /// Ensures that a DateTimeOffset field has been explicitly set.
    /// </remarks>
    /// <example>
    /// <code>
    /// RuleFor(x => x.CreatedDate).NotDefault();
    /// </code>
    /// </example>
    public static IRuleBuilderOptions<T, DateTimeOffset> NotDefault<T>(
        this IRuleBuilder<T, DateTimeOffset> validator)
    {
        // Use Must to define the validation logic.
        return validator.Must(k => k != default);
    }

    /// <summary>
    /// Validates that a Guid property is not the default (empty) value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validator">The rule builder being extended.</param>
    /// <returns>Options builder for configuring the validation rule.</returns>
    /// <remarks>
    /// Ensures that a Guid field has been explicitly set.
    /// </remarks>
    /// <example>
    /// <code>
    /// RuleFor(x => x.Id).NotDefault();
    /// </code>
    /// </example>
    public static IRuleBuilderOptions<T, Guid> NotDefault<T>(
        this IRuleBuilder<T, Guid> validator)
    {
        // Use Must to define the validation logic.
        return validator.Must(k => k != default);
    }

    #endregion
}
