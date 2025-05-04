using System.Net;

namespace Trelnex.Core.Validation;

/// <summary>
/// Exception thrown when data validation fails.
/// </summary>
/// <remarks>
/// Specialized exception for validation failures with a 422 (Unprocessable Content) status code.
/// </remarks>
/// <param name="message">The error message describing the validation failure.</param>
/// <param name="errors">A dictionary of validation errors.</param>
/// <param name="innerException">The inner exception that caused this validation failure, if any.</param>
public class ValidationException(
    string? message = null,
    IReadOnlyDictionary<string, string[]>? errors = null,
    Exception? innerException = null)
    : HttpStatusCodeException(
        httpStatusCode: HttpStatusCode.UnprocessableContent,
        message: message,
        errors: errors,
        innerException: innerException);
