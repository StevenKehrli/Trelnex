using System.Collections.Immutable;
using System.Text.RegularExpressions;
using FluentValidation.Results;

namespace Trelnex.Core.Validation;

public static partial class ValidationResultExtensions
{
    public static void ValidateOrThrow(
        this ValidationResult validationResult,
        string typeName)
    {
        if (validationResult.IsValid) return;

        var message = $"The '{typeName}' is not valid.";

        // convert the validation result to the collection of errors
        var errors = validationResult.ToErrors();

        throw new ValidationException(
            message: message,
            errors: errors);
    }

    public static void ValidateOrThrow<T>(
        this ValidationResult validationResult)
    {
        validationResult.ValidateOrThrow(typeof(T).Name);
    }

    public static void ValidateOrThrow(
        this IEnumerable<ValidationResult> validationResults,
        string typeName)
    {
        if (validationResults.All(vr => vr.IsValid)) return;

        var message = $"The collection of '{typeName}' is not valid.";

        var errors = validationResults
            // convert each ValidationResult to:
            //   (ValidationResult vr, int index)
            // we now have IEnumerable<ValidationResult vr, int index>
            .Select((vr, index) => (vr, index))
            // filter the validation results that are not valid
            // we now have IEnumerable<ValidationResult vr, int index>
            .Where(vrAndIndex => vrAndIndex.vr.IsValid is false)
            // convert each (ValidationResult vr, int index) to:
            //   (IReadOnlyDictionary<string, string[]> errors, int index)
            // we now have IEnumerable<IReadOnlyDictionary<string, string[]> errors, int index>
            .Select(vrAndIndex => (errors: vrAndIndex.vr.ToErrors(), vrAndIndex.index))
            // convert and flatten each (IReadOnlyDictionary<string, string[]> errors, int index) to:
            //   (string propertyName, string[] errors)
            // we now have IEnumerable<string propertyName, string[] errors>
            .SelectMany(errorsAndIndex =>
            {
                return errorsAndIndex.errors.Select(
                    kvp => (
                        propertyName: $"[{errorsAndIndex.index}].{kvp.Key}",
                        errors: kvp.Value));
            })
            // convert the (string propertyName, string[] errors) to a dictionary
            // where key = propertyName and value = errors
            .ToImmutableSortedDictionary(
                keySelector: kvp => kvp.propertyName,
                elementSelector: kvp => kvp.errors,
                keyComparer: new IndexedPropertyNameComparer());

        throw new ValidationException(
            message: message,
            errors: errors);
    }

    public static void ValidateOrThrow<T>(
        this IEnumerable<ValidationResult> validationResults)
    {
        validationResults.ValidateOrThrow(typeof(T).Name);
    }

    private static IReadOnlyDictionary<string, string[]> ToErrors(
        this ValidationResult validationResult)
    {
        // convert the validation result to a dictionary of key-value pairs where
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

    private partial class IndexedPropertyNameComparer : IComparer<string>
    {
        public int Compare(
            string? indexedPropertyName1,
            string? indexedPropertyName2)
        {
            // get the index and property name
            var match1 = IndexedPropertyNameRegex().Match(indexedPropertyName1!);
            var match2 = IndexedPropertyNameRegex().Match(indexedPropertyName2!);

            var index1 = int.Parse(match1.Groups["index"].Value);
            var index2 = int.Parse(match2.Groups["index"].Value);

            var indexCompare = index1.CompareTo(index2);
            if (indexCompare != 0) return indexCompare;

            return string.Compare(
                match1.Groups["propertyName"].Value,
                match2.Groups["propertyName"].Value,
                StringComparison.Ordinal);
        }

        [GeneratedRegex(@"^\[(?<index>\d+)\]\.(?<propertyName>.+)$")]
        private static partial Regex IndexedPropertyNameRegex();
    }
}
