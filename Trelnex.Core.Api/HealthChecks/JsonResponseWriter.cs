using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.HealthChecks;

/// <summary>
/// A HealthCheckOptions.ResponseWriter to json beautify the health check response.
/// https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-8.0#create-health-checks
/// </summary>
public static class JsonResponseWriter
{
    /// <summary>
    /// The options to be used with <see cref="JsonSerializer"/> to serialize the request content and deserialize the response content.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>
    /// Writes the health check response.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> representing the health check request.</param>
    /// <param name="report">The result of executing a group of IHealthCheck instances.</param>
    /// <returns>The <see cref="Task"/> representing the asynchronous operation.</returns>
    public static Task WriteResponse(
        HttpContext context,
        HealthReport report)
    {
        var json = JsonSerializer.Serialize(
            new
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
            },
            _options);

        context.Response.ContentType = MediaTypeNames.Application.Json;

        return context.Response.WriteAsync(json);
    }
}
