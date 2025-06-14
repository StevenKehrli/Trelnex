using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.DataProviders;

/// <summary>
/// Provides extension methods for adding health checks for data providers.
/// </summary>
/// <remarks>
/// Adds health checks for all data provider factories.
/// </remarks>
public static class HealthChecksExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds health checks for all registered data provider factories.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDataProviderHealthChecks(
        this IServiceCollection services)
    {
        // Find all registered data provider factories.
        var dataProviderFactories = services.GetDataProviderFactories();

        // If no data provider factories are found, return the service collection.
        if (dataProviderFactories is null)
        {
            return services;
        }

        // Get or create the health checks builder.
        var healthChecksBuilder = services.AddHealthChecks();

        // Register a health check for each data provider factory.
        foreach (var kvp in dataProviderFactories)
        {
            var providerName = kvp.Key;
            var dataProviderFactory = kvp.Value;

            // Use a consistent naming pattern for the health checks.
            var healthCheckName = $"DataProvider: {providerName}";

            // Register the health check with the builder.
            healthChecksBuilder.Add(
                new HealthCheckRegistration(
                    name: healthCheckName,
                    factory: _ => new DataProviderHealthCheck(dataProviderFactory),
                    failureStatus: null,
                    tags: null));
        }

        return services;
    }

    #endregion
}
