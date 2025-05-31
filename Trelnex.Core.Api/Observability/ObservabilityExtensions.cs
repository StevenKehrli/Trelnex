using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Trelnex.Core.Api.Configuration;

namespace Trelnex.Core.Api.Observability;

/// <summary>
/// Provides extension methods for configuring metrics, monitoring, and tracing.
/// </summary>
/// <remarks>
/// Sets up observability features.
/// </remarks>
internal static class ObservabilityExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Configures observability services for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="serviceConfiguration">The service identification details.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceConfiguration serviceConfiguration)
    {
        // Read observability configuration, using defaults if not specified.
        var observabilityConfiguration = configuration
            .GetSection("Observability")
            .Get<ObservabilityConfiguration>()
            ?? new ObservabilityConfiguration();

        // Configure Prometheus metrics collection and exposure if enabled.
        if (observabilityConfiguration.Prometheus is not null && observabilityConfiguration.Prometheus.Enabled)
        {
            // Add Prometheus metrics server on specified URL and port.
            // See: https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#kestrel-stand-alone-server
            services.AddMetricServer(options =>
            {
                options.Url = observabilityConfiguration.Prometheus.Url;
                options.Port = observabilityConfiguration.Prometheus.Port;
            });

            // Add HTTP client metrics to track outbound HTTP requests.
            // See: https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#ihttpclientfactory-metrics
            services.UseHttpClientMetrics();
        }

        // Configure OpenTelemetry distributed tracing if enabled.
        if (observabilityConfiguration.OpenTelemetry is not null && observabilityConfiguration.OpenTelemetry.Enabled)
        {
            services
                .AddOpenTelemetry()
                // Configure resource attributes for service identification.
                .ConfigureResource(resourceConfiguration =>
                {
                    resourceConfiguration.AddService(
                        serviceName: serviceConfiguration.FullName,
                        serviceVersion: serviceConfiguration.Version,
                        autoGenerateServiceInstanceId: true);
                })
                // Configure tracing with standard instrumentation.
                .WithTracing(tracingConfiguration =>
                {
                    tracingConfiguration
                        // Trace ASP.NET Core requests
                        .AddAspNetCoreInstrumentation()
                        // Trace outgoing HTTP requests
                        .AddHttpClientInstrumentation()
                        // Custom activity sources
                        .AddSource(observabilityConfiguration.OpenTelemetry.Sources)
                        // Export to OpenTelemetry Protocol endpoint
                        .AddOtlpExporter();
                });
        }

        return services;
    }

    /// <summary>
    /// Adds HTTP metrics collection middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication UseObservability(
        this WebApplication app)
    {
        // Add HTTP metrics collection middleware.
        app.UseHttpMetrics();

        return app;
    }

    #endregion

    #region Private Types

    /// <summary>
    /// Configuration model for observability features.
    /// </summary>
    private record ObservabilityConfiguration
    {
        /// <summary>
        /// Gets the Prometheus metrics server configuration.
        /// </summary>
        public PrometheusConfiguration? Prometheus { get; init; } = null!;

        /// <summary>
        /// Gets the OpenTelemetry configuration.
        /// </summary>
        public OpenTelemetryConfiguration? OpenTelemetry { get; init; } = null!;
    }

    /// <summary>
    /// Configuration model for Prometheus metrics.
    /// </summary>
    private record PrometheusConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether the Prometheus metrics server is enabled.
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Gets the URL path where metrics are exposed.
        /// </summary>
        public string Url { get; init; } = "/metrics";

        /// <summary>
        /// Gets the port number where the metrics server listens.
        /// </summary>
        public ushort Port { get; init; } = 9090;
    }

    /// <summary>
    /// Configuration model for OpenTelemetry tracing.
    /// </summary>
    private record OpenTelemetryConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether OpenTelemetry tracing is enabled.
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Gets the activity source names to include in tracing.
        /// </summary>
        public string[] Sources { get; init; } = [];
    }

    #endregion
}
