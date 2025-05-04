using System.Net;
using System.Text.RegularExpressions;

namespace Trelnex.Core;

/// <summary>
/// Extension methods for the <see cref="HttpStatusCode"/> enumeration.
/// </summary>
public static partial class HttpStatusCodeExtensions
{
    /// <summary>
    /// Converts an HTTP status code to its human-readable reason phrase.
    /// </summary>
    /// <param name="httpStatusCode">The HTTP status code to convert.</param>
    /// <returns>A human-readable reason phrase for the status code.</returns>
    /// <example>
    /// <code>
    /// HttpStatusCode.BadRequest.ToReason() => "Bad Request"
    /// HttpStatusCode.NotFound.ToReason() => "Not Found"
    /// HttpStatusCode.InternalServerError.ToReason() => "Internal Server Error"
    /// </code>
    /// </example>
    public static string ToReason(
        this HttpStatusCode httpStatusCode)
    {
        return HttpStatusCodeRegex().Replace(httpStatusCode.ToString(), " $1");
    }

    /// <summary>
    /// Regular expression to find capital letters that follow lowercase letters.
    /// </summary>
    [GeneratedRegex(@"(?<=[a-z])([A-Z])")]
    private static partial Regex HttpStatusCodeRegex();
}
