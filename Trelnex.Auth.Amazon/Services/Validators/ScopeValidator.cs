using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

public interface IScopeValidator
{
    /// <summary>
    /// Parse and validate the resource name and scope name from the specified client credential grant scope.
    /// </summary>
    /// <param name="scope">The value to parse and validate for a resource name and scope name.</param>
    /// <returns>The <see cref="ValidationResult"/> object, resource name, and scope name.</returns>
    (ValidationResult validationResult, string resourceName, string scopeName) Validate(
        string scope);
}

internal partial class ScopeValidator : BaseValidator, IScopeValidator
{
    private static readonly AbstractValidator<(string, string)> _validator = new Validator();

    /// <summary>
    /// Parse and validate the resource name and scope name from the specified client credential grant scope.
    /// </summary>
    /// <param name="scope">The value to parse and validate for a resource name and scope name.</param>
    /// <returns>The <see cref="ValidationResult"/> object, resource name, and scope name.</returns>
    public (ValidationResult validationResult, string resourceName, string scopeName) Validate(
        string scope)
    {
        var match = ScopeRegex().Match(scope);

        var instance = (
            resourceName: match.Success ? match.Groups["resourceName"].Value : null!,
            scopeName: match.Success ? match.Groups["scopeName"].Value : null!);

        return (
            validationResult: _validator.Validate(instance),
            resourceName: instance.resourceName,
            scopeName: instance.scopeName);
    }

    /// <summary>
    /// The regular expression for a scope; e.g. api://amazon.auth.trelnex.com/.default
    /// </summary>
    [GeneratedRegex(@$"^{_regexResourceName}\/{_regexScopeName}$")]
    private static partial Regex ScopeRegex();

    private class Validator : AbstractValidator<(string resourceName, string scopeName)>
    {
        public Validator()
        {
            RuleFor(x => x.resourceName)
                .NotEmpty()
                .WithMessage("resourceName is not valid.");

            RuleFor(x => x.scopeName)
                .NotEmpty()
                .WithMessage("scopeName is not valid.");
        }
    }
}
