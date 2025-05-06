using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.HealthChecks;

/// <summary>
/// Provides JSON formatting for health check endpoint responses.
/// </summary>
/// <remarks>
/// This class converts ASP.NET Core health check results into a structured, human-readable
/// JSON format. It implements the response writer pattern from the health checks framework,
/// as described in the official documentation:
/// https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-8.0#create-health-checks
///
/// The formatted output includes:
/// <list type="bullet">
///   <item>Overall status (Healthy, Degraded, Unhealthy)</item>
///   <item>Total check duration for performance monitoring</item>
///   <item>Detailed information about each individual health check</item>
/// </list>
/// </remarks>
public static class JsonResponseWriter
{
    /// <summary>
    /// JSON serialization options for formatting health check responses.
    /// </summary>
    /// <remarks>
    /// These options configure:
    /// <list type="bullet">
    ///   <item>Omission of null properties to reduce response size</item>
    ///   <item>Relaxed JSON escaping for better readability</item>
    ///   <item>camelCase property naming for JavaScript compatibility</item>
    ///   <item>Indented formatting for human readability</item>
    /// </list>
    /// </remarks>
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
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Formats the health report into a structured object</item>
    ///   <item>Serializes the object to JSON with customized formatting</item>
    ///   <item>Sets the appropriate content type on the response</item>
    ///   <item>Writes the JSON content to the response body</item>
    /// </list>
    ///
    /// The response structure includes:
    /// <list type="bullet">
    ///   <item>status: The overall health status (Healthy, Degraded, Unhealthy)</item>
    ///   <item>duration: The total time taken to execute all health checks</item>
    ///   <item>info: An array of detailed results for each individual health check</item>
    /// </list>
    ///
    /// Each health check entry in the info array includes:
    /// <list type="bullet">
    ///   <item>key: The health check name</item>
    ///   <item>description: A description of what the check verifies</item>
    ///   <item>duration: How long the individual check took</item>
    ///   <item>status: The status of this specific check</item>
    ///   <item>error: Any error message if the check failed</item>
    ///   <item>data: Additional diagnostic data provided by the check</item>
    /// </list>
    /// </remarks>
    public static Task WriteResponse(
        HttpContext context,
        HealthReport report)
    {
        // Create a structured response object
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

        // Serialize to JSON with the configured options
        var json = JsonSerializer.Serialize(responseObject, _options);

        // Set content type and write response
        context.Response.ContentType = MediaTypeNames.Application.Json;
        return context.Response.WriteAsync(json);
    }
}
