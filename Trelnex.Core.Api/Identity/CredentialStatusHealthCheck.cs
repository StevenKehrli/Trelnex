using System.Collections.Immutable;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Identity;

/// <summary>
/// Health check that monitors the status of authentication credentials.
/// </summary>
/// <param name="credentialProvider">The credential provider to monitor.</param>
/// <remarks>
/// Evaluates the health of a credential provider by examining its access tokens.
/// </remarks>
internal class CredentialStatusHealthCheck(
    ICredentialProvider credentialProvider)
    : IHealthCheck
{
    #region Public Methods

    /// <inheritdoc />
#pragma warning disable CS1998
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Get the credential status from the provider.
        var credentialStatus = credentialProvider.GetStatus();

        // Create a structured data object with token information.
        var data = new Dictionary<string, AccessTokenStatus[]>
        {
            [credentialProvider.Name] = credentialStatus.Statuses
        };

        // Determine the overall health status.
        var status = GetHealthStatus(credentialStatus.Statuses);

        // Create a health check result with detailed status information.
        var healthCheckResult = new HealthCheckResult(
            status: status,
            description: credentialProvider.Name,
            data: data.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value as object));

        return healthCheckResult;
    }
#pragma warning restore CS1998

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Determines the health status based on credential token states.
    /// </summary>
    /// <param name="accessTokenStatuses">The collection of access token statuses to evaluate.</param>
    /// <returns>The overall health status for this credential provider.</returns>
    private static HealthStatus GetHealthStatus(
        AccessTokenStatus[] accessTokenStatuses)
    {
        // If no tokens are being monitored, report as healthy.
        if (accessTokenStatuses.Length <= 0)
        {
            return HealthStatus.Healthy;
        }

        // Check if any token has expired.
        var anyExpired = accessTokenStatuses.Any(accessTokenStatus => accessTokenStatus.Health == AccessTokenHealth.Expired);

        // Report unhealthy if any token has expired.
        return anyExpired ? HealthStatus.Unhealthy : HealthStatus.Healthy;
    }

    #endregion
}
