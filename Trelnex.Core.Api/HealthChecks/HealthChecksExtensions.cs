using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace Trelnex.Core.Api.HealthChecks;

/// <summary>
/// Extension methods to add the health checks to the <see cref="IServiceCollection"/> and the <see cref="WebApplication"/>.
/// </summary>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Add the health checks to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="addHealthChecks">An optional delegate to add additional health checks to the <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services,
        Action<IHealthChecksBuilder>? addHealthChecks)
    {
        // add health checks
        var builder = services.AddHealthChecks();

        // invoke callback to caller to add health checks
        addHealthChecks?.Invoke(builder);

        // add an always healthy
        builder.AddCheck("Default", () => HealthCheckResult.Healthy());

        // expose to prometheus
        // https://github.com/prometheus-net/prometheus-net?tab=readme-ov-file#aspnet-core-health-check-status-metrics
        builder.ForwardToPrometheus();

        return services;
    }

    /// <summary>
    /// Map the health checks endpoints.
    /// </summary>
    /// <param name="erb">The <see cref="IEndpointRouteBuilder"/> to add the health checks endpoints to.</param>
    /// <returns>The <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder MapHealthChecks(
        this IEndpointRouteBuilder erb)
    {
        erb.MapHealthChecks(
            "/healthz",
            new HealthCheckOptions
            {
                ResponseWriter = JsonResponseWriter.WriteResponse
            });

        return erb;
    }
}
