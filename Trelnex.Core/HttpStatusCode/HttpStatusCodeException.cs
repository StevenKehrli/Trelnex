using System.Net;

namespace Trelnex.Core;

/// <summary>
/// Initializes a new instance of the <see cref="HttpStatusCodeException"/>.
/// with the specified <see cref="HttpStatusCode"/>, optional message, optional errors, and optional reference to the inner <see cref="Exception"/>.
/// </summary>
/// <param name="httpStatusCode">The specified <see cref="HttpStatusCode"/>.</param>
/// <param name="message">The optional error message string. If not specified, it will default to the reason phrase of the specified <see cref="HttpStatusCode"/>.</param>
/// <param name="errors">The optional dictionary of errors that describe the exception.</param>
/// <param name="innerException">The optional inner exception reference.</param>
public class HttpStatusCodeException(
    HttpStatusCode httpStatusCode,
    string? message = null,
    IReadOnlyDictionary<string, string[]>? errors = null,
    Exception? innerException = null)
    : Exception(message ?? httpStatusCode.ToReason(), innerException)
{
    public HttpStatusCode HttpStatusCode { get; init; } = httpStatusCode;

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; } = errors;
}
