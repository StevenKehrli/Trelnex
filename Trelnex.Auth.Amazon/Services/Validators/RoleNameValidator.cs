using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

public interface IRoleNameValidator
{
    /// <summary>
    /// Validate the specified role name.
    /// </summary>
    /// <param name="roleName">The specified role name.</param>
    /// <returns>The <see cref="ValidationResult"/> object and role name.</returns>
    (ValidationResult validationResult, string? roleName) Validate(
        string? roleName);
}

internal partial class RoleNameValidator : BaseValidator, IRoleNameValidator
{
    private static readonly ValidationFailure _validationFailure = new("roleName", "roleName is not valid.");

    private static readonly AbstractValidator<string> _validator = new Validator();

    /// <summary>
    /// Validate the specified role name.
    /// </summary>
    /// <param name="roleName">The specified role name.</param>
    /// <returns>The <see cref="ValidationResult"/> object and role name.</returns>
    public (ValidationResult validationResult, string? roleName) Validate(
        string? roleName)
    {
        var instance = GetInstance(roleName);

        var validationResult = instance is not null
            ? _validator.Validate(instance)
            : new ValidationResult([ _validationFailure ]);

        return (
            validationResult: validationResult,
            roleName: instance);
    }

    private static string? GetInstance(
        string? roleName)
    {
        if (roleName is null) return null!;

        var match = RoleNameRegex().Match(roleName);

        return match.Success ? match.Groups["roleName"].Value : null;
    }

    /// <summary>
    /// The regular expression for a role; e.g. .default
    /// </summary>
    [GeneratedRegex(@$"^{_regexRoleName}$")]
    private static partial Regex RoleNameRegex();

    private class Validator : AbstractValidator<string>
    {
        public Validator()
        {
            RuleFor(roleName => roleName)
                .NotEmpty()
                .WithMessage("roleName is not valid.");
        }
    }
}
