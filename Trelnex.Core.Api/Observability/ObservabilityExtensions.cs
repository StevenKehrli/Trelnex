using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

namespace Trelnex.Core.Api.Observability;

/// <summary>
/// Extension methods to add Prometheus and OpenTelemetry to the <see cref="IServiceCollection"/> and the <see cref="WebApplication"/>.
/// </summary>
internal static class ObservabilityExtensions
{
    /// <summary>
    /// Add Prometheus and OpenTelemetry to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var observabilityConfiguration = configuration
            .GetSection("Observability")
            .Get<ObservabilityConfiguration>()
            ?? new ObservabilityConfiguration();

        // add prometheus metric server
        if (observabilityConfiguration.Prometheus is not null && observabilityConfiguration.Prometheus.Enabled is true)
        {
            // add prometheus metric server
            // https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#kestrel-stand-alone-server
            services.AddMetricServer(options =>
            {
                options.Url = observabilityConfiguration.Prometheus.Url;
                options.Port = observabilityConfiguration.Prometheus.Port;
            });

            // add http client metrics
            // https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#ihttpclientfactory-metrics
            services.UseHttpClientMetrics();
        }

        // add open telemetry
        if (observabilityConfiguration.OpenTelemetry is not null && observabilityConfiguration.OpenTelemetry.Enabled is true)
        {
            services
                .AddOpenTelemetry()
                .ConfigureResource(configure =>
                {
                    configure
                        .AddService(
                            serviceName: observabilityConfiguration.OpenTelemetry.ServiceName,
                            serviceVersion: observabilityConfiguration.OpenTelemetry.ServiceVersion,
                            autoGenerateServiceInstanceId: true);
                })
                .WithTracing(configure =>
                {
                    configure
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource(observabilityConfiguration.OpenTelemetry.Sources)
                        .AddOtlpExporter();
                });
        }

        return services;
    }

    /// <summary>
    /// Configures the <see cref="WebApplication"/> to collect Prometheus metrics on process HTTP requests.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the Swagger endpoints to.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static WebApplication UseObservability(
        this WebApplication app)
    {
        app.UseHttpMetrics();

        return app;
    }

    private record ObservabilityConfiguration
    {
        /// <summary>
        /// The configuration properties for the prometheus metric server.
        /// </summary>
        public PrometheusConfiguration? Prometheus { get; init; } = null!;

        /// <summary>
        /// The configuration properties for open telemetry.
        /// </summary>
        public OpenTelemetryConfiguration? OpenTelemetry { get; init; } = null!;
    }

    /// <summary>
    /// Represents the configuration properties for the prometheus metric server.
    /// </summary>
    private record PrometheusConfiguration
    {
        /// <summary>
        /// Indicates whether the prometheus metric server is enabled.
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// The path to map the prometheus metric service endpoint.
        /// </summary>
        public string Url { get; init; } = "/metrics";

        /// <summary>
        /// The port to map the prometheus metric service endpoint.
        /// </summary>
        public ushort Port { get; init; } = 9090;
    }

    /// <summary>
    /// Represents the configuration properties for open telemetry.
    /// </summary>
    private record OpenTelemetryConfiguration
    {
        /// <summary>
        /// Indicates whether the open telemetry is enabled.
        /// </summary>
        public bool Enabled { get; init; } = false;

        /// <summary>
        /// The name of the service to be used in the telemetry.
        /// </summary>
        public string ServiceName { get; init; } = null!;

        /// <summary>
        /// The version of the service to be used in the telemetry.
        /// </summary>
        public string ServiceVersion { get; init; } = null!;

        /// <summary>
        /// The array of activity source names to be used in the telemetry.
        /// </summary>
        public string[] Sources { get; init; } = [];
    }
}
