using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.HealthChecks;

/// <summary>
/// Provides JSON formatting for health check responses.
/// </summary>
/// <remarks>
/// Converts health check results into a structured JSON format.
/// </remarks>
public static class JsonResponseWriter
{
    #region Private Static Fields

    /// <summary>
    /// JSON serialization options for formatting health check responses.
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Formats and writes a health report as a JSON response.
    /// </summary>
    /// <param name="context">The HTTP context for the health check request.</param>
    /// <param name="report">The aggregated health check results.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public static Task WriteResponse(
        HttpContext context,
        HealthReport report)
    {
        // Create a structured response object.
        var responseObject = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Info = report.Entries.Select(entry => new
            {
                Key = entry.Key,
                Description = entry.Value.Description,
                Duration = entry.Value.Duration,
                Status = Enum.GetName(entry.Value.Status),
                Error = entry.Value.Exception?.Message,
                Data = entry.Value.Data
            }).ToList()
        };

        // Serialize to JSON with the configured options.
        var json = JsonSerializer.Serialize(responseObject, _jsonSerializerOptions);

        // Set content type and write response.
        context.Response.ContentType = MediaTypeNames.Application.Json;
        return context.Response.WriteAsync(json);
    }

    #endregion
}
