using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Trelnex.Core.Api.Exceptions;

/// <summary>
/// Specialized exception handler that converts <see cref="HttpStatusCodeException"/> instances
/// into structured JSON API responses following the RFC 7807 Problem Details standard.
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
///   <item>Return a RFC 7807-compliant JSON response body using ASP.NET Core's <see cref="ProblemDetails"/></item>
/// </list>
///
/// The handler generates responses that conform to RFC 7807 (https://tools.ietf.org/html/rfc7807),
/// which defines a standard format for returning problem details in HTTP APIs.
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
    ///   <item>Use camelCase property naming for consistency with RFC 7807 and JSON conventions</item>
    /// </list>
    /// </remarks>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
    ///   <item>Creates a <see cref="ProblemDetails"/> response with RFC 7807 properties</item>
    ///   <item>Maps validation errors to the extensions dictionary</item>
    ///   <item>Sets the HTTP status code and Content-Type on the response</item>
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

        // Create a Problem Details response
        var problemDetails = new ProblemDetails
        {
            // Use the standard reason phrase for the status code as the title
            Title = httpStatusCodeException.HttpStatusCode.ToReason(),

            // Use the numeric status code from the exception
            Status = (int)httpStatusCodeException.HttpStatusCode,

            // Use the exception message as the detailed explanation
            Detail = httpStatusCodeException.Message,

            // Use the current request path as the instance URI
            Instance = httpContext.Request.Path.ToString(),

            // Map validation errors to the extensions dictionary if present
            // Otherwise use an empty dictionary
            Extensions = httpStatusCodeException.Errors?.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)kvp.Value) ?? []
        };

        // Set the HTTP status code on the response
        httpContext.Response.StatusCode = (int)httpStatusCodeException.HttpStatusCode;

        // Set the content type for RFC 7807
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        // Write the structured response as JSON
        await httpContext.Response.WriteAsJsonAsync(problemDetails, _options, cancellationToken);

        // Indicate that the exception has been handled
        return true;
    }
}
