using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.Identity;

/// <summary>
/// Provides extension methods for registering credential health checks.
/// </summary>
/// <remarks>
/// Adds health checks for all registered credential providers.
/// </remarks>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Registers health checks for all credential providers.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddIdentityHealthChecks(
        this IServiceCollection services)
    {
        // Discover all registered credential providers.
        var credentialProviders = services.GetCredentialProviders();

        // Get or create the health checks builder.
        var builder = services.AddHealthChecks();

        // Register a health check for each credential provider.
        foreach (var credentialProvider in credentialProviders)
        {
            // Use a consistent naming pattern for credential health checks.
            var healthCheckName = $"CredentialStatus: {credentialProvider.Name}";

            // Register the health check with the builder.
            builder.Add(
                new HealthCheckRegistration(
                    name: healthCheckName,
                    factory: _ => new CredentialStatusHealthCheck(credentialProvider),
                    failureStatus: null,
                    tags: null));
        }

        return services;
    }
}
