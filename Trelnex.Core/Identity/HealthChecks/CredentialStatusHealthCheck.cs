using System.Collections.Immutable;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Identity.HealthChecks;

/// <summary>
/// Initializes a new instance of the <see cref="CredentialStatusHealthCheck"/>.
/// </summary>
/// <param name="credentialProvider">The <see cref="ICredentialProvider"/> from which to get the array of <see cref="ICredentialStatusProvider"/> for this health check.</param>
internal class CredentialStatusHealthCheck(
    ICredentialProvider credentialProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        // get the credential status providers
        var credentialStatusProviders = credentialProvider.GetStatusProviders();

        // create the dictionary of credential status by the credential name
        var data = credentialStatusProviders
            .ToImmutableSortedDictionary(
                keySelector: csp => csp.CredentialName,
                elementSelector: csp => csp.GetStatus());

        // get the health status
        var status = GetHealthStatus(data);

        var healthCheckResult = new HealthCheckResult(
            status: status,
            description: credentialProvider.GetType().Name,
            data: data.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value as object));

        return Task.FromResult(healthCheckResult);
    }

    /// <summary>
    /// Gets the <see cref="HealthStatus"/> from the collection of <see cref="CredentialStatus"/>.
    /// </summary>
    /// <param name="data">The collection of <see cref="CredentialStatus"/>.</param>
    /// <returns>A <see cref="HealthStatus"/> that represents the reported status of the health check result.</returns>
    private static HealthStatus GetHealthStatus(
        IReadOnlyDictionary<string, CredentialStatus> data)
    {
        if (data.Count <= 0) return HealthStatus.Healthy;

        // enumerate each credential status
        var anyExpired = data.Any(kvp =>
        {
            // enuemrate is array of access token status
            return kvp.Value.Statuses.Any(ats => ats.Health == AccessTokenHealth.Expired);
        });

        // if any of the access tokens are expired, return unhealthy
        return anyExpired ? HealthStatus.Unhealthy : HealthStatus.Healthy;
    }
}
