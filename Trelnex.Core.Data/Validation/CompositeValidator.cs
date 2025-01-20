using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// A fluent <see cref="AbstractValidator{T}"/> that includes two validators.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
internal class CompositeValidator<T>
    : AbstractValidator<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="CompositeValidator{T}"/>.
    /// </summary>
    /// <param name="first">The first validator to include.</param>
    /// <param name="second">The second validator to include.</param>
    public CompositeValidator(
        AbstractValidator<T> first,
        AbstractValidator<T>? second = null)
    {
        Include(first);
        if (second is not null) Include(second);
    }
}
