using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Identity.HealthChecks;

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
    public static IServiceCollection AddIdentityHealthChecks(
        this IServiceCollection services)
    {
        // find any credential providers
        var credentialProviders = services.GetCredentialProviders();
        if (credentialProviders is null) return services;

        // add health checks
        var builder = services.AddHealthChecks();

        // enumerate each credential provider
        foreach (var kvp in credentialProviders)
        {
            var name = kvp.Key;
            var credentialProvider = kvp.Value;

            // get the status providers for each provider
            var credentialStatusProviders = credentialProvider.GetStatusProviders();

            // add the credential health checks
            Array.ForEach(credentialStatusProviders, credentialStatusProvider =>
            {
                // format the health check name to include the credential provider and credential name
                var healthCheckName = $"CredentialStatus: {name} {credentialStatusProvider.CredentialName}";

                builder.Add(
                    new HealthCheckRegistration(
                        name: healthCheckName,
                        factory: _ => new CredentialStatusHealthCheck(credentialStatusProvider),
                        failureStatus: null,
                        tags: null));
            });
        }

        return services;
    }
}
