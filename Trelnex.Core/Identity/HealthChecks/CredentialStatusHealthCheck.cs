using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Identity.HealthChecks;

/// <summary>
/// Initializes a new instance of the <see cref="CredentialStatusHealthCheck"/>.
/// </summary>
/// <param name="credentialStatus">The <see cref="CredentialStatus"/> from which to get the array of <see cref="AccessTokenStatus"/> for this health check.</param>
internal class CredentialStatusHealthCheck(
    ICredentialStatusProvider credentialStatusProvider) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var credentialStatus = credentialStatusProvider.GetStatus();

        var data = new Dictionary<string, object>()
        {
            ["statuses"] = credentialStatus.Statuses
        };

        var healthCheckResult = new HealthCheckResult(
            status: GetHealthStatus(credentialStatus.Statuses),
            description: credentialStatus.CredentialName,
            data: data);

        return Task.FromResult(healthCheckResult);
    }

    /// <summary>
    /// Gets the <see cref="HealthStatus"/> from the array of <see cref="AccessTokenStatus"/>.
    /// </summary>
    /// <param name="statuses">The array of <see cref="AccessTokenStatus"/>.</param>
    /// <returns>A <see cref="HealthStatus"/> that represents the reported status of the health check result.</returns>
    private static HealthStatus GetHealthStatus(
        AccessTokenStatus[] statuses)
    {
        if (statuses.Length <= 0) return HealthStatus.Unhealthy;

        return (statuses.Any(ats => ats.Health == AccessTokenHealth.Expired))
            ? HealthStatus.Unhealthy
            : HealthStatus.Healthy;
    }
}
