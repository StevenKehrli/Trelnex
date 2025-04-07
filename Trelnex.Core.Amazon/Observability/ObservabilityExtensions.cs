using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Trelnex.Core.Amazon.Observability;

/// <summary>
/// Extension methods to add Prometheus and OpenTelemetry to the <see cref="IServiceCollection"/> and the <see cref="WebApplication"/>.
/// </summary>
internal static class ObservabilityExtensions
{
    /// <summary>
    /// Add Prometheus and OpenTelemetry to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAWSInstrumentation(
        this IServiceCollection services)
    {
        services.ConfigureOpenTelemetryTracerProvider(configure =>
        {
            configure.AddAWSInstrumentation();
        });

        return services;
    }
}
