using Microsoft.Extensions.Diagnostics.HealthChecks;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.DataProviders;

/// <summary>
/// Health check that monitors the status of a data provider factory.
/// </summary>
/// <remarks>
/// Checks connectivity to the underlying data store.
/// </remarks>
/// <param name="providerFactory">The data provider factory to check status for.</param>
internal class DataProviderHealthCheck(
    IDataProviderFactory providerFactory) : IHealthCheck
{
    #region Public Methods

    /// <summary>
    /// Performs a health check on the data provider factory.
    /// </summary>
    /// <param name="context">A context object associated with the current health check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the health check.</param>
    /// <returns>
    /// A task that represents the asynchronous health check operation.
    /// </returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Get the current status from the data provider factory.
        var status = await providerFactory.GetStatusAsync(cancellationToken);

        // Convert the provider status to a health check result.
        // If the provider is healthy, return a healthy status; otherwise, return an unhealthy status.
        var healthCheckResult = new HealthCheckResult(
            status: status.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
            data: status.Data);

        return healthCheckResult;
    }

    #endregion
}
