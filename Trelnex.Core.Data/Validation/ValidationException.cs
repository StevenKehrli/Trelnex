using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Initializes a new instance of the <see cref="ValidationException"/>.
/// with the specified <see cref="HttpStatusCode"/>, optional error message, and optional reference to the inner <see cref="Exception"/>.
/// </summary>
/// <param name="message">The optional error message string. If not specified, it will default to the reason phrase of the specified <see cref="HttpStatusCode"/>.</param>
/// <param name="errors">The optional dictionary of errors that describe the exception.</param>
/// <param name="innerException">The optional inner exception reference.</param>
public class ValidationException(
    string? message = null,
    IReadOnlyDictionary<string, string[]>? errors = null,
    Exception? innerException = null)
    : HttpStatusCodeException(
        httpStatusCode: HttpStatusCode.UnprocessableContent,
        message: message,
        errors: errors,
        innerException: innerException);
