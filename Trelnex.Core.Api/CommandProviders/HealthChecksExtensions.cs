using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Provides extension methods for adding health checks for command providers.
/// </summary>
/// <remarks>
/// These extensions automatically register health checks for all command provider
/// factories in the application, enabling monitoring of data store connectivity.
/// </remarks>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Adds health checks for all registered command provider factories.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Discovers all registered command provider factories in the service collection</item>
    ///   <item>Creates and registers a health check for each factory</item>
    ///   <item>Uses a consistent naming scheme ("CommandProvider: {factory name}") for the checks</item>
    /// </list>
    ///
    /// These health checks allow monitoring of database and storage service connectivity
    /// through the application's health endpoint.
    ///
    /// If no command provider factories are registered, this method has no effect.
    /// </remarks>
    public static IServiceCollection AddCommandProviderHealthChecks(
        this IServiceCollection services)
    {
        // Find all registered command provider factories
        var commandProviderFactories = services.GetCommandProviderFactories();
        if (commandProviderFactories is null) return services;

        // Get or create the health checks builder
        var builder = services.AddHealthChecks();

        // Register a health check for each command provider factory
        foreach (var kvp in commandProviderFactories)
        {
            var name = kvp.Key;
            var commandProviderFactory = kvp.Value;

            // Use a consistent naming pattern for the health checks
            var healthCheckName = $"CommandProvider: {name}";

            // Register the health check with the builder
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
