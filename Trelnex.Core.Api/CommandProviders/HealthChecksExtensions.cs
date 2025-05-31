using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Provides extension methods for adding health checks for command providers.
/// </summary>
/// <remarks>
/// Adds health checks for all command provider factories.
/// </remarks>
public static class HealthChecksExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds health checks for all registered command provider factories.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddCommandProviderHealthChecks(
        this IServiceCollection services)
    {
        // Find all registered command provider factories.
        var commandProviderFactories = services.GetCommandProviderFactories();

        // If no command provider factories are found, return the service collection.
        if (commandProviderFactories is null)
        {
            return services;
        }

        // Get or create the health checks builder.
        var healthChecksBuilder = services.AddHealthChecks();

        // Register a health check for each command provider factory.
        foreach (var kvp in commandProviderFactories)
        {
            var providerName = kvp.Key;
            var commandProviderFactory = kvp.Value;

            // Use a consistent naming pattern for the health checks.
            var healthCheckName = $"CommandProvider: {providerName}";

            // Register the health check with the builder.
            healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    name: healthCheckName,
                    factory: _ => new CommandProviderHealthCheck(commandProviderFactory),
                    failureStatus: null,
                    tags: null));
        }

        return services;
    }

    #endregion
}
