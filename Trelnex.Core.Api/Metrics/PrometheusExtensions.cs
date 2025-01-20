using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace Trelnex.Core.Api.Metrics;

/// <summary>
/// Extension methods to add Prometheus to the <see cref="IServiceCollection"/> and the <see cref="WebApplication"/>.
/// </summary>
public static class PrometheusExtensions
{
    /// <summary>
    /// Add Prometheus to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddPrometheus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var metricsConfiguration =
            configuration.GetSection("Metrics").Get<MetricsConfiguration>() ?? new MetricsConfiguration();

        // add prometheus metric server
        // https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#kestrel-stand-alone-server
        services.AddMetricServer(options =>
        {
            options.Url = metricsConfiguration.Url;
            options.Port = metricsConfiguration.Port;
        });

        // add http client metrics
        // https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#ihttpclientfactory-metrics
        services.UseHttpClientMetrics();

        return services;
    }

    /// <summary>
    /// Configures the <see cref="WebApplication"/> to collect Prometheus metrics on process HTTP requests.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the Swagger endpoints to.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static WebApplication UsePrometheus(
        this WebApplication app)
    {
        app.UseHttpMetrics();

        return app;
    }

    /// <summary>
    /// Represents the configuration properties for the prometheus metric server.
    /// </summary>
    private record MetricsConfiguration
    {
        /// <summary>
        /// The path to map the prometheus metric service endpoint.
        /// </summary>
        public string Url { get; init; } = "/metrics";

        /// <summary>
        /// The port to map the prometheus metric service endpoint.
        /// </summary>
        public ushort Port { get; init; } = 9090;
    }
}
