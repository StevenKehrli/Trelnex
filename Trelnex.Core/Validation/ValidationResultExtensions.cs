using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FluentValidation.Results;

namespace Trelnex.Core.Validation;

/// <summary>
/// Extension methods for working with FluentValidation <see cref="ValidationResult"/> objects.
/// </summary>
/// <remarks>
/// Provides utility methods to process validation results.
/// </remarks>
public static partial class ValidationResultExtensions
{
    #region Public Methods

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the validation result contains errors.
    /// </summary>
    /// <param name="validationResult">The validation result to check.</param>
    /// <param name="typeName">The name of the type being validated, used in the error message.</param>
    /// <exception cref="ValidationException">
    /// Thrown when <see cref="ValidationResult.IsValid"/> is false, with details about all validation failures.
    /// </exception>
    public static void ValidateOrThrow(
        this ValidationResult validationResult,
        string typeName)
    {
        // If the validation result is valid, return immediately.
        if (validationResult.IsValid) return;

        var message = $"The '{typeName}' is not valid.";

        // Convert the validation result to the collection of errors.
        var errors = validationResult.ToErrors();

        // Throw a ValidationException with the message and errors.
        throw new ValidationException(
            message: message,
            errors: errors);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the validation result contains errors.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="validationResult">The validation result to check.</param>
    /// <exception cref="ValidationException">
    /// Thrown when <see cref="ValidationResult.IsValid"/> is false.
    /// </exception>
    public static void ValidateOrThrow<T>(
        this ValidationResult validationResult)
    {
        // Use the type name of T as the typeName.
        validationResult.ValidateOrThrow(typeof(T).Name);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if any validation result in the collection contains errors.
    /// </summary>
    /// <param name="validationResults">The collection of validation results to check.</param>
    /// <param name="typeName">The name of the type being validated, used in the error message.</param>
    /// <exception cref="ValidationException">
    /// Thrown when any validation result in the collection has IsValid=false.
    /// </exception>
    public static void ValidateOrThrow(
        this IEnumerable<ValidationResult> validationResults,
        string typeName)
    {
        // If all validation results are valid, return immediately.
        if (validationResults.All(vr => vr.IsValid)) return;

        var message = $"The collection of '{typeName}' is not valid.";

        var errors = validationResults
            // Convert each ValidationResult to:
            //   (ValidationResult vr, int index)
            // We now have IEnumerable<ValidationResult vr, int index>
            .Select((vr, index) => (vr, index))
            // Filter the validation results that are not valid
            // We now have IEnumerable<ValidationResult vr, int index>
            .Where(vrAndIndex => vrAndIndex.vr.IsValid is false)
            // Convert each (ValidationResult vr, int index) to:
            //   (IReadOnlyDictionary<string, string[]> errors, int index)
            // We now have IEnumerable<IReadOnlyDictionary<string, string[]> errors, int index>
            .Select(vrAndIndex => (errors: vrAndIndex.vr.ToErrors(), vrAndIndex.index))
            // Convert and flatten each (IReadOnlyDictionary<string, string[]> errors, int index) to:
            //   (string propertyName, string[] errors)
            // We now have IEnumerable<string propertyName, string[] errors>
            .SelectMany(errorsAndIndex =>
            {
                return errorsAndIndex.errors.Select(
                    kvp => (
                        propertyName: $"[{errorsAndIndex.index}].{kvp.Key}",
                        errors: kvp.Value));
            })
            // Convert the (string propertyName, string[] errors) to a dictionary
            // where key = propertyName and value = errors
            .ToImmutableSortedDictionary(
                keySelector: kvp => kvp.propertyName,
                elementSelector: kvp => kvp.errors,
                keyComparer: new IndexedPropertyNameComparer());

        // Throw a ValidationException with the message and errors.
        throw new ValidationException(
            message: message,
            errors: errors);
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if any validation result in the collection contains errors.
    /// </summary>
    /// <typeparam name="T">The type of objects being validated.</typeparam>
    /// <param name="validationResults">The collection of validation results to check.</param>
    /// <exception cref="ValidationException">
    /// Thrown when any validation result in the collection has IsValid=false.
    /// </exception>
    public static void ValidateOrThrow<T>(
        this IEnumerable<ValidationResult> validationResults)
    {
        // Use the type name of T as the typeName.
        validationResults.ValidateOrThrow(typeof(T).Name);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts a <see cref="ValidationResult"/> into a dictionary of property names and their error messages.
    /// </summary>
    /// <param name="validationResult">The validation result to convert.</param>
    /// <returns>
    /// A dictionary where keys are property names and values are arrays of error messages
    /// for each property.
    /// </returns>
    private static IReadOnlyDictionary<string, string[]> ToErrors(
        this ValidationResult validationResult)
    {
        // Convert the validation result to a dictionary of key-value pairs where
        // the key is the property name
        // the value is an array of validation error messages for that property
        //
        // g = group (ValidationFailure.PropertyName, ValidationFailure)
        // vf = ValidationFailure
        // em = ValidationFailure.ErrorMessage
        return validationResult
            .Errors
            .GroupBy(vf => vf.PropertyName)
            .ToImmutableSortedDictionary(
                keySelector: g => g.Key,
                elementSelector: g => g
                    .Select(vf => vf.ErrorMessage)
                    .OrderBy(em => em)
                    .ToArray());
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Comparer for sorting property names with array indices in validation errors.
    /// </summary>
    /// <remarks>
    /// Handles the special format "[index].PropertyName" used for collection validation errors.
    /// Sorts first by numeric index, then alphabetically by property name.
    /// This ensures consistent ordering of validation errors in collections.
    /// </remarks>
    private partial class IndexedPropertyNameComparer : IComparer<string>
    {
        #region Public Methods

        /// <summary>
        /// Compares two indexed property names for sorting.
        /// </summary>
        /// <param name="indexedPropertyName1">First property name in format "[index].PropertyName".</param>
        /// <param name="indexedPropertyName2">Second property name in format "[index].PropertyName".</param>
        /// <returns>
        /// Less than zero if first name should sort before second,
        /// zero if they sort equally,
        /// greater than zero if first name should sort after second.
        /// </returns>
        public int Compare(
            string? indexedPropertyName1,
            string? indexedPropertyName2)
        {
            // Get the index and property name from the first property name.
            var match1 = IndexedPropertyNameRegex().Match(indexedPropertyName1!);
            // Get the index and property name from the second property name.
            var match2 = IndexedPropertyNameRegex().Match(indexedPropertyName2!);

            // Parse the index from the first match.
            var index1 = int.Parse(match1.Groups["index"].Value);
            // Parse the index from the second match.
            var index2 = int.Parse(match2.Groups["index"].Value);

            // Compare the indices.
            var indexCompare = index1.CompareTo(index2);
            // If the indices are not equal, return the result of the comparison.
            if (indexCompare != 0) return indexCompare;

            // If the indices are equal, compare the property names.
            return string.Compare(
                match1.Groups["propertyName"].Value,
                match2.Groups["propertyName"].Value,
                StringComparison.Ordinal);
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Regular expression for extracting index and property name from indexed property path.
        /// </summary>
        /// <returns>A regex pattern matching the format "[index].PropertyName".</returns>
        [GeneratedRegex(@"^\[(?<index>\d+)\]\.(?<propertyName>.+)$")]
        private static partial Regex IndexedPropertyNameRegex();

        #endregion
    }

    #endregion
}
