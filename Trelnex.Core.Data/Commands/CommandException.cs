using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Exception for command execution errors.
/// </summary>
/// <remarks>
/// Provides command exceptions with HTTP status codes.
/// </remarks>
/// <param name="httpStatusCode">HTTP status code for this exception.</param>
/// <param name="message">Optional error message.</param>
/// <param name="innerException">Optional inner exception.</param>
public class CommandException(
    HttpStatusCode httpStatusCode,
    string? message = null,
    Exception? innerException = null)
    : HttpStatusCodeException(
        httpStatusCode: httpStatusCode,
        message: message,
        innerException: innerException);
