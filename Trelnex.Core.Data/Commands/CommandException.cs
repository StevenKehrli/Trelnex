using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents an exception that occurs during command execution in the data access layer.
/// </summary>
/// <remarks>
/// This exception type inherits from <see cref="HttpStatusCodeException"/> and provides
/// a standard way to communicate exceptions from data commands with appropriate HTTP status codes.
/// It's typically used by command handlers, repositories, and services to translate data-related
/// errors to HTTP-compatible error representations.
/// </remarks>
/// <param name="httpStatusCode">The HTTP status code associated with this exception.</param>
/// <param name="message">The optional error message string. If not specified, it will default to the reason phrase of the specified <see cref="HttpStatusCode"/>.</param>
/// <param name="innerException">The optional inner exception reference.</param>
/// <seealso cref="HttpStatusCodeException"/>
public class CommandException(
    HttpStatusCode httpStatusCode,
    string? message = null,
    Exception? innerException = null)
    : HttpStatusCodeException(
        httpStatusCode: httpStatusCode,
        message: message,
        innerException: innerException);
