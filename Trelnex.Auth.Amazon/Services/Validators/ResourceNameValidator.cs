using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;

namespace Trelnex.Auth.Amazon.Services.Validators;

/// <summary>
/// Defines operations for validating resource names in the RBAC system.
/// </summary>
/// <remarks>
/// This interface abstracts the validation of resource names, allowing for dependency injection
/// and easier unit testing. Resource names must adhere to specific format requirements to ensure
/// consistency and security within the Role-Based Access Control (RBAC) system.
/// </remarks>
public interface IResourceNameValidator
{
    /// <summary>
    /// Validates the specified resource name against format requirements.
    /// </summary>
    /// <param name="resourceName">The resource name to validate.</param>
    /// <returns>
    /// A tuple containing:
    /// - A <see cref="ValidationResult"/> object indicating success or listing validation errors
    /// - The normalized resource name if valid; otherwise, <see langword="null"/>
    /// </returns>
    /// <remarks>
    /// This method checks that resource names adhere to the URI format requirements defined by
    /// the RBAC system (e.g., "api://amazon.auth.trelnex.com"). If validation passes, the
    /// normalized form of the resource name is returned for further processing.
    /// </remarks>
    (ValidationResult validationResult, string? resourceName) Validate(
        string? resourceName);
}

/// <summary>
/// Validates resource names to ensure they meet the format requirements of the RBAC system.
/// </summary>
/// <remarks>
/// This validator ensures that resource names follow a URI format typical in OAuth 2.0 and
/// OpenID Connect applications. Valid resource names are URIs that start with specific schemes
/// (api, http, urn) followed by a properly formatted identifier.
///
/// The validator uses a two-step process:
/// 1. First, a regular expression match confirms the overall format
/// 2. Then, additional validation rules confirm the content meets all requirements
///
/// Examples of valid resource names:
/// - api://amazon.auth.trelnex.com
/// - http://example.com/resources/reports
/// - urn://authenticated-service.example
///
/// This class leverages FluentValidation for clear, composable validation rules.
/// </remarks>
internal partial class ResourceNameValidator : BaseValidator, IResourceNameValidator
{
    #region Private Static Fields

    /// <summary>
    /// Standard validation failure for invalid resource names.
    /// </summary>
    private static readonly ValidationFailure _validationFailure = new("resourceName", "resourceName is not valid.");

    /// <summary>
    /// FluentValidation validator instance with resource name validation rules.
    /// </summary>
    private static readonly AbstractValidator<string> _validator = new Validator();

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates the specified resource name against format requirements.
    /// </summary>
    /// <param name="resourceName">The resource name to validate.</param>
    /// <returns>
    /// A tuple containing:
    /// - A <see cref="ValidationResult"/> object indicating success or listing validation errors
    /// - The normalized resource name if valid; otherwise, <see langword="null"/>
    /// </returns>
    /// <remarks>
    /// This implementation performs a series of checks:
    /// 1. Extracts a valid resource name using regex pattern matching
    /// 2. If extraction fails, returns a validation failure
    /// 3. If extraction succeeds, applies additional validation rules using FluentValidation
    /// 4. Returns both the validation result and the normalized resource name
    ///
    /// The normalized resource name is used throughout the RBAC system to ensure consistent
    /// representation of resources. If validation fails, a null resource name is returned.
    /// </remarks>
    public (ValidationResult validationResult, string? resourceName) Validate(
        string? resourceName)
    {
        // Extract a valid resource name using regex pattern matching.
        var instance = GetInstance(resourceName);

        // If extraction fails, return a validation failure; otherwise, apply additional validation rules using FluentValidation.
        var validationResult = instance is not null
            ? _validator.Validate(instance)
            : new ValidationResult([ _validationFailure ]);

        return (
            validationResult: validationResult,
            resourceName: instance);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Extracts and normalizes a resource name from the input string.
    /// </summary>
    /// <param name="resourceName">The input resource name to process.</param>
    /// <returns>
    /// The normalized resource name if the input matches the required pattern;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method applies the resource name regex pattern to the input string.
    /// If the pattern matches, it extracts the named capture group "resourceName"
    /// as the normalized form. This normalization ensures consistent representation
    /// of resource names throughout the system.
    /// </remarks>
    private static string? GetInstance(
        string? resourceName)
    {
        // If the resource name is null, return null.
        if (resourceName is null) return null!;

        // Apply the resource name regex pattern to the input string.
        var match = ResourceNameRegex().Match(resourceName);

        // If the pattern matches, return the extracted resource name; otherwise, return null.
        return match.Success ? match.Groups["resourceName"].Value : null;
    }

    /// <summary>
    /// Returns a compiled regular expression for validating resource names.
    /// </summary>
    /// <returns>A compiled regular expression matching valid resource name patterns.</returns>
    /// <remarks>
    /// This method provides a regular expression that matches valid resource name patterns
    /// such as "api://amazon.auth.trelnex.com". The pattern is anchored with start (^) and
    /// end ($) markers to ensure the entire string matches the pattern.
    ///
    /// This is implemented using C# source generators to create an efficient compiled regex.
    /// </remarks>
    [GeneratedRegex(@$"^{_regexResourceName}$")]
    private static partial Regex ResourceNameRegex();

    #endregion

    #region Nested Classes

    /// <summary>
    /// Provides FluentValidation rules for resource name validation.
    /// </summary>
    /// <remarks>
    /// This nested validator class defines additional validation rules for resource names
    /// beyond the basic pattern matching. It ensures that resource names are not empty
    /// and meet any other business rules required for the RBAC system.
    /// </remarks>
    private class Validator : AbstractValidator<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class with resource name validation rules.
        /// </summary>
        public Validator()
        {
            // Resource name must not be empty.
            RuleFor(resourceName => resourceName)
                .NotEmpty()
                .WithMessage("resourceName is not valid.");
        }
    }

    #endregion
}
