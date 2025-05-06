using FluentValidation;

namespace Trelnex.Core.Validation;

/// <summary>
/// A composite validator that combines multiple FluentValidation validators.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
/// <remarks>
/// Enables combining multiple validators into a single pipeline.
/// </remarks>
public class CompositeValidator<T>
    : AbstractValidator<T>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeValidator{T}"/> class.
    /// </summary>
    /// <param name="first">The primary validator (required).</param>
    /// <param name="second">The secondary validator (optional).</param>
    public CompositeValidator(
        IValidator<T> first,
        IValidator<T>? second = null)
    {
        // Include the rules from the first validator.
        Include(first);

        // If a second validator is provided, include its rules as well.
        if (second is not null) Include(second);
    }

    #endregion
}
