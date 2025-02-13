using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

public interface IScopeNameValidator
{
    /// <summary>
    /// Determines if the specified scope name is the default scope.
    /// </summary>
    /// <param name="scopeName">The specified scope name.</param>
    /// <returns><see langword="true"/> if the specified scope name is the default scope; otherwise, <see langword="false"/>.</returns>
    bool IsDefault(string? scopeName);

    /// <summary>
    /// Validate the specified scope name.
    /// </summary>
    /// <param name="scopeName">The specified scope name.</param>
    /// <returns>The <see cref="ValidationResult"/> object and scope name.</returns>
    (ValidationResult validationResult, string? scopeName) Validate(
        string? scopeName);
}

internal partial class ScopeNameValidator : BaseValidator, IScopeNameValidator
{
    private static readonly ValidationFailure _validationFailure = new("scopeName", "scopeName is not valid.");

    private static readonly AbstractValidator<string> _validator = new Validator();

    /// <summary>
    /// Determines if the specified scope name is the default scope.
    /// </summary>
    /// <param name="scopeName">The specified scope name.</param>
    /// <returns><see langword="true"/> if the specified scope name is the default scope; otherwise, <see langword="false"/>.</returns>
    public bool IsDefault(string? scopeName) => scopeName == ".default";

    /// <summary>
    /// Validate the specified scope name.
    /// </summary>
    /// <param name="scopeName">The specified scope name.</param>
    /// <returns>The <see cref="ValidationResult"/> object and scope name.</returns>
    public (ValidationResult validationResult, string? scopeName) Validate(
        string? scopeName)
    {
        var instance = GetInstance(scopeName);

        var validationResult = instance is not null
            ? _validator.Validate(instance)
            : new ValidationResult([ _validationFailure ]);

        return (
            validationResult: validationResult,
            scopeName: instance);
    }

    private static string? GetInstance(
        string? scopeName)
    {
        if (scopeName is null) return null!;

        var match = ScopeNameRegex().Match(scopeName);

        return match.Success ? match.Groups["scopeName"].Value : null;
    }

    /// <summary>
    /// The regular expression for a scope; e.g. .default
    /// </summary>
    [GeneratedRegex(@$"^{_regexScopeName}$")]
    private static partial Regex ScopeNameRegex();

    private class Validator : AbstractValidator<string>
    {
        public Validator()
        {
            RuleFor(scopeName => scopeName)
                .NotEmpty()
                .WithMessage("scopeName is not valid.");
        }
    }
}
