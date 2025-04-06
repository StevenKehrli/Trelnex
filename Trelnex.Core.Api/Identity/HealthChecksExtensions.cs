using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Api.Identity;

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

        // add health checks
        var builder = services.AddHealthChecks();

        // enumerate each credential provider
        foreach (var credentialProvider in credentialProviders)
        {
            var healthCheckName = $"CredentialStatus: {credentialProvider.Name}";

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
