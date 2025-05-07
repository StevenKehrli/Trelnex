using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Trelnex.Core.Api.Exceptions;

/// <summary>
/// Exception handler that converts <see cref="HttpStatusCodeException"/> instances into structured JSON API responses.
/// </summary>
/// <remarks>
/// Integrates with ASP.NET Core's exception handling middleware to provide consistent HTTP error responses.
/// </remarks>
public class HttpStatusCodeExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// JSON serialization options for formatting error responses.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Attempts to handle the specified exception.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// <see langword="true"/> if the exception was handled successfully; otherwise, <see langword="false"/>.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Only handle exceptions of the specific HTTP status code type.
        if (exception is not HttpStatusCodeException httpStatusCodeException) return false;

        // Create a Problem Details response.
        var problemDetails = new ProblemDetails
        {
            // Use the standard reason phrase for the status code as the title.
            Title = httpStatusCodeException.HttpStatusCode.ToReason(),

            // Use the numeric status code from the exception.
            Status = (int)httpStatusCodeException.HttpStatusCode,

            // Use the exception message as the detailed explanation.
            Detail = httpStatusCodeException.Message,

            // Use the current request path as the instance URI.
            Instance = httpContext.Request.Path.ToString(),

            // Map validation errors to the extensions dictionary if present; otherwise, use an empty dictionary.
            Extensions = httpStatusCodeException.Errors?.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)kvp.Value) ?? []
        };

        // Set the HTTP status code on the response.
        httpContext.Response.StatusCode = (int)httpStatusCodeException.HttpStatusCode;

        // Set the content type for RFC 7807.
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        // Write the structured response as JSON.
        await httpContext.Response.WriteAsJsonAsync(problemDetails, _options, cancellationToken);

        // Indicate that the exception has been handled.
        return true;
    }
}
