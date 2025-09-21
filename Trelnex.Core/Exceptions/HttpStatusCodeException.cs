using System.Collections.Immutable;
using System.Net;
using System.Text.Json.Nodes;

namespace Trelnex.Core.Exceptions;

/// <summary>
/// Exception that represents an HTTP error with associated status code and optional structured error details.
/// Supports both typed string array errors and flexible JSON object errors for different API versions.
/// </summary>
public class HttpStatusCodeException : Exception
{
    private static readonly IReadOnlyDictionary<string, object?> s_emptyErrors = ImmutableDictionary<string, object?>.Empty;

    #region Constructor

    /// <summary>
    /// Protected base constructor for internal use by factory methods.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
    /// <param name="message">The error message.</param>
    /// <param name="errors">Optional structured validation errors.</param>
    /// <param name="innerException">Optional inner exception that caused this HTTP error.</param>
    protected HttpStatusCodeException(
        HttpStatusCode httpStatusCode,
        string? message,
        IReadOnlyDictionary<string, string[]>? errors,
        Exception? innerException)
        : base(message ?? httpStatusCode.ToReason(), innerException)
    {
        HttpStatusCode = httpStatusCode;

        Errors = errors?.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value) ?? s_emptyErrors;
    }

    /// <summary>
    /// Protected constructor for internal use by factory methods with JSON object errors.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
    /// <param name="message">The error message.</param>
    /// <param name="errors">Optional JSON object containing arbitrary error structure.</param>
    /// <param name="innerException">Optional inner exception that caused this HTTP error.</param>
    protected HttpStatusCodeException(
        HttpStatusCode httpStatusCode,
        string? message,
        JsonObject? errors,
        Exception? innerException)
        : base(message ?? httpStatusCode.ToReason(), innerException)
    {
        HttpStatusCode = httpStatusCode;

        Errors = errors?.ToImmutableDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value) ?? s_emptyErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpStatusCodeException"/> class with basic error information.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
    /// <param name="message">Optional error message. If not provided, defaults to the standard reason phrase for the HTTP status code.</param>
    /// <param name="innerException">Optional inner exception that caused this HTTP error.</param>
    public HttpStatusCodeException(
        HttpStatusCode httpStatusCode,
        string? message = null,
        Exception? innerException = null)
        : base(message ?? httpStatusCode.ToReason(), innerException)
    {
        HttpStatusCode = httpStatusCode;
        Errors = s_emptyErrors;
    }

    /// <summary>
    /// Creates a new <see cref="HttpStatusCodeException"/> with typed string array errors.
    /// Typically used for validation errors in v1 APIs.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
    /// <param name="message">Optional error message. If not provided, defaults to the standard reason phrase for the HTTP status code.</param>
    /// <param name="errors">Dictionary of structured validation errors with field names as keys and error message arrays as values.</param>
    /// <param name="innerException">Optional inner exception that caused this HTTP error.</param>
    /// <returns>A new <see cref="HttpStatusCodeException"/> instance with typed errors.</returns>
    public static HttpStatusCodeException WithErrors(
        HttpStatusCode httpStatusCode,
        string? message = null,
        IReadOnlyDictionary<string, string[]>? errors = null,
        Exception? innerException = null)
    {
        return new HttpStatusCodeException(httpStatusCode, message, errors, innerException);
    }

    /// <summary>
    /// Creates a new <see cref="HttpStatusCodeException"/> with flexible JSON object errors.
    /// Typically used for preserving original JSON error structure from external API responses.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
    /// <param name="message">Optional error message. If not provided, defaults to the standard reason phrase for the HTTP status code.</param>
    /// <param name="errors">JSON object containing arbitrary error structure that will be preserved in responses.</param>
    /// <param name="innerException">Optional inner exception that caused this HTTP error.</param>
    /// <returns>A new <see cref="HttpStatusCodeException"/> instance with JSON errors.</returns>
    public static HttpStatusCodeException WithJsonObject(
        HttpStatusCode httpStatusCode,
        string? message = null,
        JsonObject? errors = null,
        Exception? innerException = null)
    {
        return new HttpStatusCodeException(httpStatusCode, message, errors, innerException);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the HTTP status code that should be returned in the response for this exception.
    /// </summary>
    public HttpStatusCode HttpStatusCode { get; init; }

    /// <summary>
    /// Gets the errors formatted for ProblemDetails responses.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Errors { get; init; }

    #endregion
}