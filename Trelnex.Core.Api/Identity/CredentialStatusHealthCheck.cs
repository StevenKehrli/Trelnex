using System.Collections.Immutable;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Identity;

/// <summary>
/// Health check implementation that monitors the status of authentication credentials.
/// </summary>
/// <remarks>
/// This health check evaluates the health of a credential provider by examining
/// the status of its access tokens. It reports credential expiration issues
/// that could affect the application's ability to access protected resources.
/// </remarks>
internal class CredentialStatusHealthCheck : IHealthCheck
{
    private readonly ICredentialProvider _credentialProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialStatusHealthCheck"/> class.
    /// </summary>
    /// <param name="credentialProvider">The credential provider to monitor.</param>
    /// <remarks>
    /// Each health check instance monitors a single credential provider,
    /// allowing for independent monitoring of different authentication mechanisms.
    /// </remarks>
    internal CredentialStatusHealthCheck(ICredentialProvider credentialProvider)
    {
        _credentialProvider = credentialProvider;
    }

    /// <summary>
    /// Performs a health check on the credential provider.
    /// </summary>
    /// <param name="context">A context object associated with the current health check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the health check.</param>
    /// <returns>
    /// A task that represents the asynchronous health check operation. The result contains
    /// the health check status and diagnostic information about the credential provider.
    /// </returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Retrieves the credential status from the provider</item>
    ///   <item>Creates a structured data object with token health information</item>
    ///   <item>Determines the overall health status based on token expiration</item>
    ///   <item>Returns a health check result with detailed status information</item>
    /// </list>
    ///
    /// The health status will be Unhealthy if any access token is expired,
    /// as this indicates potential authentication failures for protected resources.
    /// </remarks>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Get the credential status from the provider
        var credentialStatus = _credentialProvider.GetStatus();

        // Create a structured data object with token information
        var data = new Dictionary<string, AccessTokenStatus[]>
        {
            [_credentialProvider.Name] = credentialStatus.Statuses
        };

        // Determine the overall health status
        var status = GetHealthStatus(credentialStatus.Statuses);

        // Create a health check result with detailed status information
        var healthCheckResult = new HealthCheckResult(
            status: status,
            description: _credentialProvider.Name,
            data: data.ToImmutableSortedDictionary(kvp => kvp.Key, kvp => kvp.Value as object));

        return Task.FromResult(healthCheckResult);
    }

    /// <summary>
    /// Determines the health status based on credential token states.
    /// </summary>
    /// <param name="statuses">The collection of access token statuses to evaluate.</param>
    /// <returns>The overall health status for this credential provider.</returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    ///   <item>Reports Healthy if no access tokens are being monitored</item>
    ///   <item>Reports Unhealthy if any token has expired, which indicates authentication will fail</item>
    ///   <item>Reports Healthy if all tokens are valid and not expired</item>
    /// </list>
    /// </remarks>
    private static HealthStatus GetHealthStatus(
        AccessTokenStatus[] statuses)
    {
        // If no tokens are being monitored, report as healthy
        if (statuses.Length <= 0) return HealthStatus.Healthy;

        // Check if any token has expired
        var anyExpired = statuses.Any(ats => ats.Health == AccessTokenHealth.Expired);

        // Report unhealthy if any token has expired
        return anyExpired ? HealthStatus.Unhealthy : HealthStatus.Healthy;
    }
}
