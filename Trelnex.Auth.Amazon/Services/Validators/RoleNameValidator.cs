using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

/// <summary>
/// Defines operations for validating role names in the RBAC system.
/// </summary>
/// <remarks>
/// This interface abstracts the validation of role names, allowing for dependency injection
/// and easier unit testing. Role names must adhere to specific format requirements to ensure
/// consistency and security within the Role-Based Access Control (RBAC) system.
/// </remarks>
public interface IRoleNameValidator
{
    /// <summary>
    /// Validates the specified role name against format requirements.
    /// </summary>
    /// <param name="roleName">The role name to validate.</param>
    /// <returns>
    /// A tuple containing:
    /// - A <see cref="ValidationResult"/> object indicating success or listing validation errors
    /// - The normalized role name if valid; otherwise, <see langword="null"/>
    /// </returns>
    /// <remarks>
    /// This method checks that role names adhere to the format requirements defined by
    /// the RBAC system (e.g., "admin.read"). If validation passes, the normalized form
    /// of the role name is returned for further processing.
    /// </remarks>
    (ValidationResult validationResult, string? roleName) Validate(
        string? roleName);
}

/// <summary>
/// Validates role names to ensure they meet the format requirements of the RBAC system.
/// </summary>
/// <remarks>
/// This validator ensures that role names follow the required format for the RBAC system.
/// Valid role names consist of lowercase alphanumeric characters, dots, and hyphens,
/// typically following a pattern like "category.permission-level".
///
/// The validator uses a two-step process:
/// 1. First, a regular expression match confirms the overall format
/// 2. Then, additional validation rules confirm the content meets all requirements
///
/// Examples of valid role names:
/// - service.read
/// - admin.full-access
/// - reporting.view-only
///
/// Role names are critical in the RBAC system as they define the permission sets that
/// can be assigned to principals for specific resources. Consistent naming conventions
/// ensure clear understanding of permissions throughout the system.
///
/// This class leverages FluentValidation for clear, composable validation rules.
/// </remarks>
internal partial class RoleNameValidator : BaseValidator, IRoleNameValidator
{
    #region Private Static Fields

    /// <summary>
    /// Standard validation failure for invalid role names.
    /// </summary>
    private static readonly ValidationFailure _validationFailure = new("roleName", "roleName is not valid.");

    /// <summary>
    /// FluentValidation validator instance with role name validation rules.
    /// </summary>
    private static readonly AbstractValidator<string> _validator = new Validator();

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates the specified role name against format requirements.
    /// </summary>
    /// <param name="roleName">The role name to validate.</param>
    /// <returns>
    /// A tuple containing:
    /// - A <see cref="ValidationResult"/> object indicating success or listing validation errors
    /// - The normalized role name if valid; otherwise, <see langword="null"/>
    /// </returns>
    /// <remarks>
    /// This implementation performs a series of checks:
    /// 1. Extracts a valid role name using regex pattern matching
    /// 2. If extraction fails, returns a validation failure
    /// 3. If extraction succeeds, applies additional validation rules using FluentValidation
    /// 4. Returns both the validation result and the normalized role name
    ///
    /// The normalized role name is used throughout the RBAC system to ensure consistent
    /// representation of roles. If validation fails, a null role name is returned.
    /// </remarks>
    public (ValidationResult validationResult, string? roleName) Validate(
        string? roleName)
    {
        // Extract a valid role name using regex pattern matching.
        var instance = GetInstance(roleName);

        // If extraction fails, return a validation failure; otherwise, apply additional validation rules using FluentValidation.
        var validationResult = instance is not null
            ? _validator.Validate(instance)
            : new ValidationResult([ _validationFailure ]);

        return (
            validationResult: validationResult,
            roleName: instance);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Extracts and normalizes a role name from the input string.
    /// </summary>
    /// <param name="roleName">The input role name to process.</param>
    /// <returns>
    /// The normalized role name if the input matches the required pattern;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method applies the role name regex pattern to the input string.
    /// If the pattern matches, it extracts the named capture group "roleName"
    /// as the normalized form. This normalization ensures consistent representation
    /// of role names throughout the system.
    /// </remarks>
    private static string? GetInstance(
        string? roleName)
    {
        // If the role name is null, return null.
        if (roleName is null) return null!;

        // Apply the role name regex pattern to the input string.
        var match = RoleNameRegex().Match(roleName);

        // If the pattern matches, return the extracted role name; otherwise, return null.
        return match.Success ? match.Groups["roleName"].Value : null;
    }

    /// <summary>
    /// Returns a compiled regular expression for validating role names.
    /// </summary>
    /// <returns>A compiled regular expression matching valid role name patterns.</returns>
    /// <remarks>
    /// This method provides a regular expression that matches valid role name patterns
    /// such as "service.read" or "admin.full-access". The pattern is anchored with
    /// start (^) and end ($) markers to ensure the entire string matches the pattern.
    ///
    /// This is implemented using C# source generators to create an efficient compiled regex.
    /// </remarks>
    [GeneratedRegex(@$"^{_regexRoleName}$")]
    private static partial Regex RoleNameRegex();

    #endregion

    #region Nested Classes

    /// <summary>
    /// Provides FluentValidation rules for role name validation.
    /// </summary>
    /// <remarks>
    /// This nested validator class defines additional validation rules for role names
    /// beyond the basic pattern matching. It ensures that role names are not empty
    /// and meet any other business rules required for the RBAC system.
    /// </remarks>
    private class Validator : AbstractValidator<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class with role name validation rules.
        /// </summary>
        public Validator()
        {
            // Role name must not be empty.
            RuleFor(roleName => roleName)
                .NotEmpty()
                .WithMessage("roleName is not valid.");
        }
    }

    #endregion
}
