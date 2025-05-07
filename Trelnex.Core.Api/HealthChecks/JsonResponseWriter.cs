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
    /// <summary>
    /// JSON serialization options for formatting health check responses.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

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
            Info = report.Entries
                .Select(e =>
                    new
                    {
                        Key = e.Key,
                        Description = e.Value.Description,
                        Duration = e.Value.Duration,
                        Status = Enum.GetName(
                            typeof(HealthStatus),
                            e.Value.Status),
                        Error = e.Value.Exception?.Message,
                        Data = e.Value.Data
                    })
                .ToList()
        };

        // Serialize to JSON with the configured options.
        var json = JsonSerializer.Serialize(responseObject, _options);

        // Set content type and write response.
        context.Response.ContentType = MediaTypeNames.Application.Json;
        return context.Response.WriteAsync(json);
    }
}
