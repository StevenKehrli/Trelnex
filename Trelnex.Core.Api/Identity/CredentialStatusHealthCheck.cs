using System.Collections.Immutable;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Identity;

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
        // get the credential status
        var credentialStatus = credentialProvider.GetStatus();

        // create the dictionary of credential status by the credential name
        var data = new Dictionary<string, AccessTokenStatus[]>
        {
            [ credentialProvider.Name ] = credentialStatus.Statuses
        };

        // get the health status
        var status = GetHealthStatus(credentialStatus.Statuses);

        var healthCheckResult = new HealthCheckResult(
            status: status,
            description: credentialProvider.Name,
            data: data.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value as object));

        return Task.FromResult(healthCheckResult);
    }

    /// <summary>
    /// Gets the <see cref="HealthStatus"/> from the collection of <see cref="CredentialStatus"/>.
    /// </summary>
    /// <param name="data">The collection of <see cref="CredentialStatus"/>.</param>
    /// <returns>A <see cref="HealthStatus"/> that represents the reported status of the health check result.</returns>
    private static HealthStatus GetHealthStatus(
        AccessTokenStatus[] statuses)
    {
        if (statuses.Length <= 0) return HealthStatus.Healthy;

        // enumerate each access token status
        var anyExpired = statuses.Any(ats => ats.Health == AccessTokenHealth.Expired);

        // if any of the access tokens are expired, return unhealthy
        return anyExpired ? HealthStatus.Unhealthy : HealthStatus.Healthy;
    }
}
