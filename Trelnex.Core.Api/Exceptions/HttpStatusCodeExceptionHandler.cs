using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Trelnex.Core.Exceptions;

namespace Trelnex.Core.Api.Exceptions;

/// <summary>
/// Exception handler that converts <see cref="HttpStatusCodeException"/> instances into structured Problem Details JSON responses.
/// </summary>
/// <remarks>
/// Integrates with ASP.NET Core's exception handling middleware to provide consistent HTTP error responses
/// conforming to RFC 7807 Problem Details specification. This handler extracts HTTP status codes and
/// structured error information from exceptions to create standardized API error responses.
/// </remarks>
public class HttpStatusCodeExceptionHandler : IExceptionHandler
{
    #region Private Static Fields

    /// <summary>
    /// JSON serialization options configured for Problem Details responses with camelCase naming and null omission.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to handle the specified exception by converting it to a Problem Details response.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// <see langword="true"/> if the exception was an <see cref="HttpStatusCodeException"/> and was handled;
    /// otherwise, <see langword="false"/> to allow other handlers to process the exception.
    /// </returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Only handle HttpStatusCodeException instances; let other handlers process different exception types
        if (exception is not HttpStatusCodeException httpStatusCodeException) return false;

        // Build RFC 7807 Problem Details response from the exception
        var problemDetails = new ProblemDetails
        {
            // Standard reason phrase for the HTTP status code (e.g., "Bad Request", "Not Found")
            Title = httpStatusCodeException.HttpStatusCode.ToReason(),

            // Numeric HTTP status code from the exception
            Status = (int)httpStatusCodeException.HttpStatusCode,

            // Detailed error message from the exception
            Detail = httpStatusCodeException.Message,

            // Current request path for problem instance identification
            Instance = httpContext.Request.Path.ToString(),

            // Include structured validation errors in extensiojs if available
            Extensions = httpStatusCodeException.Errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };

        // Configure response with appropriate status code and content type
        httpContext.Response.StatusCode = (int)httpStatusCodeException.HttpStatusCode;
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;

        // Serialize Problem Details as JSON response
        await httpContext.Response.WriteAsJsonAsync(problemDetails, _jsonSerializerOptions, cancellationToken);

        // Signal successful handling to prevent further exception processing
        return true;
    }

    #endregion
}