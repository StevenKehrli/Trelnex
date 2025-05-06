using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Trelnex.Core.Api.Responses;

namespace Trelnex.Core.Api.Exceptions;

/// <summary>
/// Specialized exception handler that converts <see cref="HttpStatusCodeException"/> instances
/// into structured JSON API responses.
/// </summary>
/// <remarks>
/// This handler integrates with ASP.NET Core's exception handling middleware to provide
/// consistent HTTP error responses across the application. It ensures that exceptions
/// with HTTP status information are properly transformed into standardized API responses
/// with appropriate status codes and error details.
///
/// When registered with the exception handling middleware, this handler will:
/// <list type="bullet">
///   <item>Detect exceptions of type <see cref="HttpStatusCodeException"/></item>
///   <item>Extract status code, message, and error details</item>
///   <item>Set the appropriate HTTP status code on the response</item>
///   <item>Return a JSON-formatted error response body</item>
/// </list>
/// </remarks>
public class HttpStatusCodeExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// JSON serialization options for formatting error responses.
    /// </summary>
    /// <remarks>
    /// These options:
    /// <list type="bullet">
    ///   <item>Skip null values to keep responses compact</item>
    ///   <item>Use relaxed JSON escaping for better readability</item>
    /// </list>
    /// </remarks>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Attempts to handle the specified exception by converting it to a structured API response.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the exception was handled successfully; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Checks if the exception is of type <see cref="HttpStatusCodeException"/></item>
    ///   <item>Creates a structured response with status code, message, and error details</item>
    ///   <item>Sets the HTTP status code on the response</item>
    ///   <item>Writes the structured response as JSON to the response body</item>
    /// </list>
    ///
    /// If the exception is not of the expected type, the method returns false,
    /// allowing other handlers or the default error handling to process it.
    /// </remarks>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Only handle exceptions of the specific HTTP status code type
        if (exception is not HttpStatusCodeException httpStatusCodeException) return false;

        // Create a structured response from the exception
        var httpStatusCodeResponse = new HttpStatusCodeResponse
        {
            StatusCode = (int)httpStatusCodeException.HttpStatusCode,
            Message = httpStatusCodeException.Message,
            Errors = httpStatusCodeException.Errors,
        };

        // Set the HTTP status code on the response
        httpContext.Response.StatusCode = httpStatusCodeResponse.StatusCode;

        // Write the structured response as JSON
        await httpContext.Response.WriteAsJsonAsync(httpStatusCodeResponse, _options, cancellationToken);

        // Indicate that the exception has been handled
        return true;
    }
}
