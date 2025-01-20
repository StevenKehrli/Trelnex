using System.Net;

namespace Trelnex.Core.Data;

/// <summary>
/// Initializes a new instance of the <see cref="CommandException"/>
/// with the specified <see cref="HttpStatusCode"/>, optional error message, and optional reference to the inner <see cref="Exception"/>.
/// </summary>
/// <param name="httpStatusCode">The specified <see cref="HttpStatusCode"/>.</param>
/// <param name="message">The optional error message string. If not specified, it will default to the reason phrase of the specified <see cref="HttpStatusCode"/>.</param>
/// <param name="innerException">The optional inner exception reference.</param>
public class CommandException(
    HttpStatusCode httpStatusCode,
    string? message = null,
    Exception? innerException = null)
    : HttpStatusCodeException(
        httpStatusCode: httpStatusCode,
        message: message,
        innerException: innerException);
