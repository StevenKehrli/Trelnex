using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace Trelnex.Core.Api.HealthChecks;

/// <summary>
/// Provides extension methods for configuring health checks.
/// </summary>
/// <remarks>
/// Simplifies setup of health monitoring.
/// </remarks>
public static class HealthChecksExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers health check services with customizable health checks.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDefaultHealthChecks(
        this IServiceCollection services)
    {
        // Register health check services.
        var healthChecksBuilder = services.AddHealthChecks();

        // Add a default health check that always returns healthy.
        // This ensures the /healthz endpoint works even without custom checks.
        healthChecksBuilder.AddCheck("Default", () => HealthCheckResult.Healthy());

        // Configure Prometheus metrics integration to expose health check status as metrics.
        // See: https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#aspnet-core-health-check-status-metrics
        healthChecksBuilder.ForwardToPrometheus();

        return services;
    }

    /// <summary>
    /// Maps the health check endpoint to the application pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to configure.</param>
    /// <returns>The endpoint route builder for method chaining.</returns>
    public static IEndpointRouteBuilder MapHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/healthz", // Standard Kubernetes health check path
            new HealthCheckOptions
            {
                // Use custom JSON response writer for readable, structured output.
                ResponseWriter = JsonResponseWriter.WriteResponse
            });

        return endpoints;
    }

    #endregion
}
