using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Represents an exception that occurs during command execution with an associated HTTP status code.
/// </summary>
/// <param name="httpStatusCode">The HTTP status code that describes the error.</param>
/// <param name="message">Optional error message describing the exception.</param>
/// <param name="innerException">Optional inner exception that caused this exception.</param>
public class CommandException(
    HttpStatusCode httpStatusCode,
    string? message = null,
    Exception? innerException = null)
    : HttpStatusCodeException(
        httpStatusCode: httpStatusCode,
        message: message,
        innerException: innerException);
