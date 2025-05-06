using Microsoft.Extensions.Diagnostics.HealthChecks;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Health check implementation that monitors the status of a command provider factory.
/// </summary>
/// <remarks>
/// This health check queries the given command provider factory for its status,
/// which typically reflects connectivity to the underlying data store such as
/// a database or cloud storage service.
///
/// Each health check instance monitors a single command provider factory,
/// allowing for independent monitoring of different data stores.
/// </remarks>
/// <param name="providerFactory">The command provider factory to check status for.</param>
internal class CommandProviderHealthCheck(
    ICommandProviderFactory providerFactory) : IHealthCheck
{
    /// <summary>
    /// Performs a health check on the command provider factory.
    /// </summary>
    /// <param name="context">A context object associated with the current health check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the health check.</param>
    /// <returns>
    /// A task that represents the asynchronous health check operation. The result contains
    /// the health check status and any additional diagnostic information.
    /// </returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Queries the command provider factory for its current status</item>
    ///   <item>Maps the provider status to an appropriate health check status</item>
    ///   <item>Includes any diagnostic data from the provider in the result</item>
    /// </list>
    /// The returned health status will be Healthy or Unhealthy based on the
    /// provider factory's connection to its data store.
    /// </remarks>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Get the current status from the command provider factory
        var status = providerFactory.GetStatus();

        // Convert the provider status to a health check result
        var healthCheckResult = new HealthCheckResult(
            status: status.IsHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
            data: status.Data);

        return Task.FromResult(healthCheckResult);
    }
}
