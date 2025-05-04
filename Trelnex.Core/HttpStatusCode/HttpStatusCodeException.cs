using System.Net;

namespace Trelnex.Core;

/// <summary>
/// Exception that represents an HTTP error with associated status code.
/// </summary>
/// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
/// <param name="message">Optional error message. Defaults to the reason phrase of the status code if not specified.</param>
/// <param name="errors">Optional dictionary of structured validation errors.</param>
/// <param name="innerException">Optional inner exception that caused this exception.</param>
public class HttpStatusCodeException(
    HttpStatusCode httpStatusCode,
    string? message = null,
    IReadOnlyDictionary<string, string[]>? errors = null,
    Exception? innerException = null)
    : Exception(message ?? httpStatusCode.ToReason(), innerException)
{
    /// <summary>
    /// Gets the HTTP status code associated with this exception.
    /// </summary>
    public HttpStatusCode HttpStatusCode { get; init; } = httpStatusCode;

    /// <summary>
    /// Gets the structured validation errors associated with this exception.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; } = errors;
}
