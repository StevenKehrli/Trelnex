using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Data.HealthChecks;

/// <summary>
/// Extension methods to add the health checks to the <see cref="IServiceCollection"/> and the <see cref="WebApplication"/>.
/// </summary>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Add the health checks to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCommandProviderHealthChecks(
        this IServiceCollection services)
    {
        // find any command provider factories
        var commandProviderFactories = services.GetCommandProviderFactories();
        if (commandProviderFactories is null) return services;

        // add health checks
        var builder = services.AddHealthChecks();

        // enumerate each command provider factory
        foreach (var kvp in commandProviderFactories)
        {
            var name = kvp.Key;
            var commandProviderFactory = kvp.Value;

            // format the health check name to include the cosmos command provider factory name
            var healthCheckName = $"CommandProvider: {name}";

            builder.Add(
                new HealthCheckRegistration(
                    name: healthCheckName,
                    factory: _ => new CommandProviderHealthCheck(commandProviderFactory),
                    failureStatus: null,
                    tags: null));
        }

        return services;
    }
}
