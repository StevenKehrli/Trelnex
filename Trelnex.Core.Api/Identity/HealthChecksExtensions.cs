using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.Identity;

/// <summary>
/// Provides extension methods for registering credential health checks.
/// </summary>
/// <remarks>
/// These extensions automatically add health checks for all registered credential providers,
/// enabling monitoring of authentication token health and credential status.
/// </remarks>
public static class HealthChecksExtensions
{
    /// <summary>
    /// Registers health checks for all credential providers.
    /// </summary>
    /// <param name="services">The service collection to add health checks to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Discovers all registered credential providers in the service collection</item>
    ///   <item>Creates and registers a health check for each provider</item>
    ///   <item>Uses a consistent naming scheme ("CredentialStatus: {provider name}") for the checks</item>
    /// </list>
    ///
    /// These health checks monitor the status of authentication tokens, reporting when
    /// tokens have expired or are nearing expiration. This helps detect authentication
    /// issues before they cause service disruptions.
    ///
    /// If no credential providers are registered, this method has no effect.
    /// </remarks>
    public static IServiceCollection AddIdentityHealthChecks(
        this IServiceCollection services)
    {
        // Discover all registered credential providers
        var credentialProviders = services.GetCredentialProviders();

        // Get or create the health checks builder
        var builder = services.AddHealthChecks();

        // Register a health check for each credential provider
        foreach (var credentialProvider in credentialProviders)
        {
            // Use a consistent naming pattern for credential health checks
            var healthCheckName = $"CredentialStatus: {credentialProvider.Name}";

            // Register the health check with the builder
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
