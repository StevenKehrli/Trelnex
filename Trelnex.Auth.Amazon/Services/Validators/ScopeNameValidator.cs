using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

/// <summary>
/// Defines operations for validating scope names in the RBAC system.
/// </summary>
/// <remarks>
/// This interface abstracts the validation of scope names, allowing for dependency injection
/// and easier unit testing. Scope names must adhere to specific format requirements to ensure
/// consistency and security within the Role-Based Access Control (RBAC) system.
///
/// Scopes represent authorization boundaries or contexts in which permissions are valid,
/// such as environments (dev, test, prod), regions, or logical domains.
/// </remarks>
public interface IScopeNameValidator
{
    /// <summary>
    /// Determines if the specified scope name is the default scope.
    /// </summary>
    /// <param name="scopeName">The scope name to check.</param>
    /// <returns><see langword="true"/> if the scope name is the special ".default" scope; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The ".default" scope is a special scope in OAuth 2.0 and OpenID Connect that represents
    /// the default set of permissions requested by a client. In the RBAC system, this scope
    /// can be used to apply roles across all available authorization boundaries for a resource.
    /// </remarks>
    bool IsDefault(string? scopeName);

    /// <summary>
    /// Validates the specified scope name against format requirements.
    /// </summary>
    /// <param name="scopeName">The scope name to validate.</param>
    /// <returns>
    /// A tuple containing:
    /// - A <see cref="ValidationResult"/> object indicating success or listing validation errors
    /// - The normalized scope name if valid; otherwise, <see langword="null"/>
    /// </returns>
    /// <remarks>
    /// This method checks that scope names adhere to the format requirements defined by
    /// the RBAC system (e.g., "rbac" or ".default"). If validation passes, the normalized
    /// form of the scope name is returned for further processing.
    /// </remarks>
    (ValidationResult validationResult, string? scopeName) Validate(
        string? scopeName);
}

/// <summary>
/// Validates scope names to ensure they meet the format requirements of the RBAC system.
/// </summary>
/// <remarks>
/// This validator ensures that scope names follow the required format for the RBAC system.
/// Valid scope names consist of lowercase alphanumeric characters, dots, and hyphens,
/// representing different authorization boundaries within which roles can be exercised.
///
/// The validator uses a two-step process:
/// 1. First, a regular expression match confirms the overall format
/// 2. Then, additional validation rules confirm the content meets all requirements
///
/// Examples of valid scope names:
/// - .default (the standard default scope)
/// - rbac
/// - development
/// - production
///
/// Scopes are essential in the RBAC system as they define the authorization boundaries
/// within which roles are valid. For example, a user might have "admin" permissions, but
/// only within the "development" scope, not in "production".
///
/// This class leverages FluentValidation for clear, composable validation rules.
/// </remarks>
internal partial class ScopeNameValidator : BaseValidator, IScopeNameValidator
{
    #region Private Static Fields

    /// <summary>
    /// Standard validation failure for invalid scope names.
    /// </summary>
    private static readonly ValidationFailure _validationFailure = new("scopeName", "scopeName is not valid.");

    /// <summary>
    /// FluentValidation validator instance with scope name validation rules.
    /// </summary>
    private static readonly AbstractValidator<string> _validator = new Validator();

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines if the specified scope name is the default scope (".default").
    /// </summary>
    /// <param name="scopeName">The scope name to check.</param>
    /// <returns><see langword="true"/> if the scope name is the special ".default" scope; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The ".default" scope has special meaning in OAuth 2.0 and OpenID Connect systems.
    /// In the RBAC context, it may represent a scope that applies across all authorization boundaries.
    /// This method provides a convenient way to identify this special scope for special handling.
    /// </remarks>
    public bool IsDefault(string? scopeName) => scopeName == ".default";

    /// <summary>
    /// Validates the specified scope name against format requirements.
    /// </summary>
    /// <param name="scopeName">The scope name to validate.</param>
    /// <returns>
    /// A tuple containing:
    /// - A <see cref="ValidationResult"/> object indicating success or listing validation errors
    /// - The normalized scope name if valid; otherwise, <see langword="null"/>
    /// </returns>
    /// <remarks>
    /// This implementation performs a series of checks:
    /// 1. Extracts a valid scope name using regex pattern matching
    /// 2. If extraction fails, returns a validation failure
    /// 3. If extraction succeeds, applies additional validation rules using FluentValidation
    /// 4. Returns both the validation result and the normalized scope name
    ///
    /// The normalized scope name is used throughout the RBAC system to ensure consistent
    /// representation of scopes. If validation fails, a null scope name is returned.
    /// </remarks>
    public (ValidationResult validationResult, string? scopeName) Validate(
        string? scopeName)
    {
        // Extract and normalize the scope name from the input string.
        var instance = GetInstance(scopeName);

        // If extraction fails, return a validation failure; otherwise, validate the extracted scope name.
        var validationResult = instance is not null
            ? _validator.Validate(instance)
            : new ValidationResult([ _validationFailure ]);

        return (
            validationResult: validationResult,
            scopeName: instance);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Extracts and normalizes a scope name from the input string.
    /// </summary>
    /// <param name="scopeName">The input scope name to process.</param>
    /// <returns>
    /// The normalized scope name if the input matches the required pattern;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method applies the scope name regex pattern to the input string.
    /// If the pattern matches, it extracts the named capture group "scopeName"
    /// as the normalized form. This normalization ensures consistent representation
    /// of scope names throughout the system.
    /// </remarks>
    private static string? GetInstance(
        string? scopeName)
    {
        // If the scope name is null, return null.
        if (scopeName is null) return null!;

        // Apply the scope name regex pattern to the input string.
        var match = ScopeNameRegex().Match(scopeName);

        // If the pattern matches, return the extracted scope name; otherwise, return null.
        return match.Success ? match.Groups["scopeName"].Value : null;
    }

    /// <summary>
    /// Returns a compiled regular expression for validating scope names.
    /// </summary>
    /// <returns>A compiled regular expression matching valid scope name patterns.</returns>
    /// <remarks>
    /// This method provides a regular expression that matches valid scope name patterns
    /// such as ".default" or "rbac". The pattern is anchored with start (^) and
    /// end ($) markers to ensure the entire string matches the pattern.
    ///
    /// This is implemented using C# source generators to create an efficient compiled regex.
    /// </remarks>
    [GeneratedRegex(@$"^{_regexScopeName}$")]
    private static partial Regex ScopeNameRegex();

    #endregion

    #region Nested Classes

    /// <summary>
    /// Provides FluentValidation rules for scope name validation.
    /// </summary>
    /// <remarks>
    /// This nested validator class defines additional validation rules for scope names
    /// beyond the basic pattern matching. It ensures that scope names are not empty
    /// and meet any other business rules required for the RBAC system.
    /// </remarks>
    private class Validator : AbstractValidator<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class with scope name validation rules.
        /// </summary>
        public Validator()
        {
            // Scope name must not be empty.
            RuleFor(scopeName => scopeName)
                .NotEmpty()
                .WithMessage("scopeName is not valid.");
        }
    }

    #endregion
}
