using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Trelnex.Core.Data.HealthChecks;

/// <summary>
/// Initializes a new instance of the <see cref="CommandProviderHealthCheck"/>.
/// </summary>
/// <param name="providerFactory">The <see cref="ICommandProviderFactory"/> to get the status.</param>
internal class CommandProviderHealthCheck(
    ICommandProviderFactory providerFactory) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var status = providerFactory.GetStatus();

        var healthCheckResult = new HealthCheckResult(
            status: status.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
            data: status.Data);

        return Task.FromResult(healthCheckResult);
    }
}
