using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

/// <summary>
/// Defines operations for validating and parsing OAuth/OpenID Connect scope strings.
/// </summary>
/// <remarks>
/// This interface provides functionality to validate and parse scope strings used in
/// OAuth 2.0 client credentials flow, extracting the resource name and scope name components.
/// Scope strings typically follow the format "resourceName/scopeName", such as "api://service/.default".
/// </remarks>
public interface IScopeValidator
{
    /// <summary>
    /// Parses and validates the resource name and scope name from the specified client credential grant scope.
    /// </summary>
    /// <param name="scope">The scope string to parse and validate (format: "resourceName/scopeName").</param>
    /// <returns>A tuple containing the validation result, extracted resource name, and scope name.</returns>
    /// <remarks>
    /// The method extracts the resource name and scope name components using regex pattern matching,
    /// then validates both components according to the defined validation rules.
    /// Even if validation fails, the extracted components are still returned along with the validation result.
    /// </remarks>
    (ValidationResult validationResult, string resourceName, string scopeName) Validate(
        string scope);
}

/// <summary>
/// Implements scope string validation and parsing for OAuth/OpenID Connect scope formats.
/// </summary>
/// <remarks>
/// This validator extracts and validates resource names and scope names from scope strings
/// using regular expression pattern matching and FluentValidation rules.
/// It inherits base regex patterns from <see cref="BaseValidator"/> for consistent
/// validation across the authentication service.
/// </remarks>
internal partial class ScopeValidator : BaseValidator, IScopeValidator
{
    #region Private Static Fields

    /// <summary>
    /// The static validator instance used to validate extracted resource and scope names.
    /// </summary>
    private static readonly AbstractValidator<(string, string)> _validator = new Validator();

    #endregion

    #region Public Methods

    /// <summary>
    /// Parses and validates the resource name and scope name from the specified client credential grant scope.
    /// </summary>
    /// <param name="scope">The scope string to parse and validate (format: "resourceName/scopeName").</param>
    /// <returns>A tuple containing the validation result, extracted resource name, and scope name.</returns>
    /// <remarks>
    /// The implementation follows these steps:
    /// 1. Apply regex pattern matching to extract resourceName and scopeName components
    /// 2. Create a tuple with the extracted components (null if matching fails)
    /// 3. Validate the tuple using FluentValidation rules
    /// 4. Return the validation result along with the extracted components
    ///
    /// Example valid scopes:
    /// - "api://amazon.auth.trelnex.com/.default"
    /// - "https://myservice.com/read-data"
    /// </remarks>
    public (ValidationResult validationResult, string resourceName, string scopeName) Validate(
        string scope)
    {
        // Apply regex pattern matching to extract components.
        var match = ScopeRegex().Match(scope);

        // Create a tuple with the extracted components.
        // If the regex match fails, the components will be null.
        var instance = (
            resourceName: match.Success ? match.Groups["resourceName"].Value : null!,
            scopeName: match.Success ? match.Groups["scopeName"].Value : null!);

        // Validate and return components with validation result.
        return (
            validationResult: _validator.Validate(instance),
            resourceName: instance.resourceName,
            scopeName: instance.scopeName);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Provides a regular expression for validating scope format.
    /// </summary>
    /// <returns>A compiled regular expression that matches valid scope strings.</returns>
    /// <remarks>
    /// The regex pattern matches strings in the format "resourceName/scopeName" where:
    /// - resourceName follows the pattern defined in BaseValidator._regexResourceName
    /// - scopeName follows the pattern defined in BaseValidator._regexScopeName
    ///
    /// Example matches:
    /// - "api://amazon.auth.trelnex.com/.default"
    /// - "https://myservice.com/read-data"
    /// </remarks>
    [GeneratedRegex(@$"^{_regexResourceName}\/{_regexScopeName}$")]
    private static partial Regex ScopeRegex();

    #endregion

    #region Nested Types

    /// <summary>
    /// Provides validation rules for resource name and scope name components.
    /// </summary>
    /// <remarks>
    /// This validator uses FluentValidation to define rules for validating
    /// the components extracted from scope strings.
    /// </remarks>
    private class Validator : AbstractValidator<(string resourceName, string scopeName)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        /// <remarks>
        /// Configures validation rules for both the resource name and scope name components.
        /// Both components are required to be non-empty strings.
        /// </remarks>
        public Validator()
        {
            // Resource name must not be empty.
            RuleFor(x => x.resourceName)
                .NotEmpty()
                .WithMessage("resourceName is not valid.");

            // Scope name must not be empty.
            RuleFor(x => x.scopeName)
                .NotEmpty()
                .WithMessage("scopeName is not valid.");
        }
    }

    #endregion
}
