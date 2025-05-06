using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace Trelnex.Core.Api.HealthChecks;

/// <summary>
/// Provides extension methods for configuring health checks and health check endpoints.
/// </summary>
/// <remarks>
/// These extensions simplify the setup of health monitoring for applications,
/// providing both service registration and endpoint mapping in a standardized way.
/// Health checks are used for monitoring application status, readiness, and dependencies
/// in both development and production environments.
/// </remarks>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Registers health check services with customizable health checks.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <param name="addHealthChecks">Optional delegate to register application-specific health checks.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Configures the core health check services</item>
    ///   <item>Invokes the provided delegate to register application-specific health checks</item>
    ///   <item>Adds a default health check that always returns healthy (as a fallback)</item>
    ///   <item>Configures Prometheus metrics integration for health check status monitoring</item>
    /// </list>
    ///
    /// Use the optional delegate to register health checks for dependencies such as
    /// databases, message queues, external services, or any application-specific
    /// components that should be monitored for proper operation.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks(builder => {
    ///     builder.AddSqlServer(connectionString, name: "database");
    ///     builder.AddUrlGroup(new Uri("https://api.example.com"), name: "example-api");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        Action<IHealthChecksBuilder>? addHealthChecks)
    {
        // Register health check services
        var builder = services.AddHealthChecks();

        // Allow application to register custom health checks
        addHealthChecks?.Invoke(builder);

        // Add a default health check that always returns healthy
        // This ensures the /healthz endpoint works even without custom checks
        builder.AddCheck("Default", () => HealthCheckResult.Healthy());

        // Configure Prometheus metrics integration to expose health check status as metrics
        // See: https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#aspnet-core-health-check-status-metrics
        builder.ForwardToPrometheus();

        return services;
    }

    /// <summary>
    /// Maps the health check endpoint to the application pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to configure.</param>
    /// <returns>The endpoint route builder for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    ///   <item>Exposes health checks at the "/healthz" endpoint (Kubernetes convention)</item>
    ///   <item>Configures JSON formatting for health check responses using <see cref="JsonResponseWriter"/></item>
    /// </list>
    ///
    /// The health check endpoint can be used by:
    /// <list type="bullet">
    ///   <item>Container orchestrators (like Kubernetes) for readiness/liveness probes</item>
    ///   <item>Load balancers to determine if an instance should receive traffic</item>
    ///   <item>Monitoring systems to track application health</item>
    ///   <item>Developers to diagnose application issues</item>
    /// </list>
    /// </remarks>
    public static IEndpointRouteBuilder MapHealthChecks(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(
            "/healthz",  // Standard Kubernetes health check path
            new HealthCheckOptions
            {
                // Use custom JSON response writer for readable, structured output
                ResponseWriter = JsonResponseWriter.WriteResponse
            });

        return endpoints;
    }
}
