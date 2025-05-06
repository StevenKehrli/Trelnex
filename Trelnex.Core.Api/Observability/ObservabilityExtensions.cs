using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Trelnex.Core.Api.Configuration;

namespace Trelnex.Core.Api.Observability;

/// <summary>
/// Provides extension methods for configuring metrics, monitoring, and tracing capabilities.
/// </summary>
/// <remarks>
/// These extensions set up observability features including:
/// <list type="bullet">
///   <item>Prometheus metrics collection and exposure</item>
///   <item>OpenTelemetry distributed tracing</item>
///   <item>HTTP client metrics and instrumentation</item>
/// </list>
///
/// They integrate with standard observability platforms while keeping configuration
/// centralized and consistent across applications.
/// </remarks>
internal static class ObservabilityExtensions
{
    /// <summary>
    /// Configures observability services for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration containing observability settings.</param>
    /// <param name="serviceConfiguration">The service identification details used for telemetry.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Sets up Prometheus metrics if enabled in configuration</item>
    ///   <item>Configures OpenTelemetry distributed tracing if enabled</item>
    ///   <item>Registers HTTP client metrics for monitoring outbound requests</item>
    /// </list>
    ///
    /// Configuration is read from the "Observability" section in application settings.
    /// If this section is missing, observability features will be disabled by default.
    /// </remarks>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceConfiguration serviceConfiguration)
    {
        // Read observability configuration, using defaults if not specified
        var observabilityConfiguration = configuration
            .GetSection("Observability")
            .Get<ObservabilityConfiguration>()
            ?? new ObservabilityConfiguration();

        // Configure Prometheus metrics collection and exposure if enabled
        if (observabilityConfiguration.Prometheus is not null && observabilityConfiguration.Prometheus.Enabled is true)
        {
            // Add Prometheus metrics server on specified URL and port
            // See: https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#kestrel-stand-alone-server
            services.AddMetricServer(options =>
            {
                options.Url = observabilityConfiguration.Prometheus.Url;
                options.Port = observabilityConfiguration.Prometheus.Port;
            });

            // Add HTTP client metrics to track outbound HTTP requests
            // See: https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#ihttpclientfactory-metrics
            services.UseHttpClientMetrics();
        }

        // Configure OpenTelemetry distributed tracing if enabled
        if (observabilityConfiguration.OpenTelemetry is not null && observabilityConfiguration.OpenTelemetry.Enabled is true)
        {
            services
                .AddOpenTelemetry()
                // Configure resource attributes for service identification
                .ConfigureResource(configure =>
                {
                    configure
                        .AddService(
                            serviceName: serviceConfiguration.FullName,
                            serviceVersion: serviceConfiguration.Version,
                            autoGenerateServiceInstanceId: true);
                })
                // Configure tracing with standard instrumentation
                .WithTracing(configure =>
                {
                    configure
                        .AddAspNetCoreInstrumentation()  // Trace ASP.NET Core requests
                        .AddHttpClientInstrumentation()  // Trace outgoing HTTP requests
                        .AddSource("Trelnex.*")          // Trace Trelnex library activity
                        .AddSource(observabilityConfiguration.OpenTelemetry.Sources)  // Custom activity sources
                        .AddOtlpExporter();              // Export to OpenTelemetry Protocol endpoint
                });
        }

        return services;
    }

    /// <summary>
    /// Adds HTTP metrics collection middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for method chaining.</returns>
    /// <remarks>
    /// This method adds middleware that automatically collects metrics for
    /// all HTTP requests processed by the application, including:
    /// <list type="bullet">
    ///   <item>Request counters with status code and endpoint labels</item>
    ///   <item>Request duration histograms</item>
    ///   <item>In-progress request gauges</item>
    /// </list>
    ///
    /// These metrics are exposed through the Prometheus endpoint and can be
    /// used for monitoring, alerting, and performance analysis.
    /// </remarks>
    public static WebApplication UseObservability(
        this WebApplication app)
    {
        // Add HTTP metrics collection middleware
        app.UseHttpMetrics();

        return app;
    }

    /// <summary>
    /// Configuration model for observability features.
    /// </summary>
    /// <remarks>
    /// This record represents the structure expected in the "Observability"
    /// configuration section, with separate subsections for different
    /// observability technologies.
    /// </remarks>
    private record ObservabilityConfiguration
    {
        /// <summary>
        /// Gets the Prometheus metrics server configuration.
        /// </summary>
        /// <value>
        /// Configuration for the Prometheus metrics endpoint, or <see langword="null"/> to disable.
        /// </value>
        public PrometheusConfiguration? Prometheus { get; init; } = null!;

        /// <summary>
        /// Gets the OpenTelemetry configuration.
        /// </summary>
        /// <value>
        /// Configuration for OpenTelemetry tracing, or <see langword="null"/> to disable.
        /// </value>
        public OpenTelemetryConfiguration? OpenTelemetry { get; init; } = null!;
    }

    /// <summary>
    /// Configuration model for Prometheus metrics.
    /// </summary>
    /// <remarks>
    /// This record defines settings for the Prometheus metrics server,
    /// which exposes application metrics in a format that can be scraped
    /// by a Prometheus server.
    /// </remarks>
    private record PrometheusConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether the Prometheus metrics server is enabled.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to enable the metrics server; otherwise, <see langword="false"/>.
        /// </value>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Gets the URL path where metrics are exposed.
        /// </summary>
        /// <value>
        /// The relative URL path for the metrics endpoint. Defaults to "/metrics".
        /// </value>
        public string Url { get; init; } = "/metrics";

        /// <summary>
        /// Gets the port number where the metrics server listens.
        /// </summary>
        /// <value>
        /// The port number for the metrics server. Defaults to 9090.
        /// </value>
        public ushort Port { get; init; } = 9090;
    }

    /// <summary>
    /// Configuration model for OpenTelemetry tracing.
    /// </summary>
    /// <remarks>
    /// This record defines settings for OpenTelemetry distributed tracing,
    /// which enables tracking of requests across service boundaries and
    /// visualization of request flows and dependencies.
    /// </remarks>
    private record OpenTelemetryConfiguration
    {
        /// <summary>
        /// Gets a value indicating whether OpenTelemetry tracing is enabled.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to enable distributed tracing; otherwise, <see langword="false"/>.
        /// </value>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// Gets the activity source names to include in tracing.
        /// </summary>
        /// <value>
        /// An array of activity source names representing components to be instrumented.
        /// </value>
        /// <remarks>
        /// These are additional activity sources beyond the standard ones that are
        /// automatically included (ASP.NET Core, HTTP client, etc.).
        /// </remarks>
        public string[] Sources { get; init; } = [];
    }
}
