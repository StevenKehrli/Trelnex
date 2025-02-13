using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

public interface IResourceNameValidator
{
    /// <summary>
    /// Validate the specified resource name.
    /// </summary>
    /// <param name="resourceName">The specified resource name.</param>
    /// <returns>The <see cref="ValidationResult"/> object and resource name.</returns>
    (ValidationResult validationResult, string? resourceName) Validate(
        string? resourceName);
}

internal partial class ResourceNameValidator : BaseValidator, IResourceNameValidator
{
    private static readonly ValidationFailure _validationFailure = new("resourceName", "resourceName is not valid.");

    private static readonly AbstractValidator<string> _validator = new Validator();

    /// <summary>
    /// Validate the specified resource name.
    /// </summary>
    /// <param name="resourceName">The specified resource name.</param>
    /// <returns>The <see cref="ValidationResult"/> object and resource name.</returns>
    public (ValidationResult validationResult, string? resourceName) Validate(
        string? resourceName)
    {
        var instance = GetInstance(resourceName);

        var validationResult = instance is not null
            ? _validator.Validate(instance)
            : new ValidationResult([ _validationFailure ]);

        return (
            validationResult: validationResult,
            resourceName: instance);
    }

    private static string? GetInstance(
        string? resourceName)
    {
        if (resourceName is null) return null!;

        var match = ResourceNameRegex().Match(resourceName);

        return match.Success ? match.Groups["resourceName"].Value : null;
    }

    /// <summary>
    /// The regular expression for a scope; e.g. api://amazon.auth.trelnex.com
    /// </summary>
    [GeneratedRegex(@$"^{_regexResourceName}$")]
    private static partial Regex ResourceNameRegex();

    private class Validator : AbstractValidator<string>
    {
        public Validator()
        {
            RuleFor(resourceName => resourceName)
                .NotEmpty()
                .WithMessage("resourceName is not valid.");
        }
    }
}
