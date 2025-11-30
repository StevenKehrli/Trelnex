using System.Diagnostics;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Static helper class for creating AWS credentials with proactive automatic refresh.
/// </summary>
/// <remarks>
/// <para>
/// Wraps AWS SDK <see cref="RefreshingAWSCredentials"/> with a proactive refresh scheduler
/// to ensure credentials are refreshed before expiration.
/// </para>
/// <para>
/// The AWS SDK only checks for credential expiration reactively when <c>GetCredentials()</c> is called.
/// This helper ensures credentials are refreshed proactively via background scheduling.
/// </para>
/// </remarks>
internal static class AWSCredentialsManager
{
    #region Public Static Methods

    /// <summary>
    /// Creates AWS credentials from the default credential chain with automatic proactive refresh.
    /// </summary>
    /// <param name="logger">The logger for recording refresh operations.</param>
    /// <returns>
    /// AWS credentials that will automatically refresh before expiration.
    /// Returns <see cref="RefreshingCredentials"/> wrapper for refreshing credentials,
    /// or the original credentials for static credentials.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Uses <see cref="DefaultAWSCredentialsIdentityResolver"/> to obtain credentials from the standard AWS credential chain
    /// (environment variables, profile, IAM role, etc.).
    /// </para>
    /// <para>
    /// If credentials are <see cref="RefreshingAWSCredentials"/>, wraps them in <see cref="RefreshingCredentials"/>
    /// to enable proactive refresh scheduling. Otherwise returns credentials as-is after initialization.
    /// </para>
    /// </remarks>
    public static AWSCredentials CreateAWSCredentials(
        ILogger logger)
    {
        // Resolve credentials from the default AWS credential chain
        var awsCredentials = DefaultAWSCredentialsIdentityResolver.GetCredentials();

        // Wrap refreshing credentials with proactive refresh scheduler
        if (awsCredentials is RefreshingAWSCredentials refreshingAWSCredentials)
        {
            return new RefreshingCredentials(logger, refreshingAWSCredentials);
        }

        // Initialize and return static credentials (no refresh needed)
        _ = awsCredentials.GetCredentials();

        return awsCredentials;
    }

    #endregion

    #region RefreshingCredentials

    /// <summary>
    /// Wrapper for <see cref="RefreshingAWSCredentials"/> that proactively schedules credential refreshes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The AWS SDK's <see cref="RefreshingAWSCredentials"/> only checks the preempt expiry window
    /// reactively when <c>GetCredentials()</c> is called. This can lead to expired credentials if
    /// no requests are made during the preempt window.
    /// </para>
    /// <para>
    /// This wrapper proactively schedules refreshes using an async <see cref="Task.Delay"/> pattern,
    /// ensuring credentials are refreshed before they expire regardless of request activity.
    /// </para>
    /// <para>
    /// Refresh timing is calculated as: expiration time - <see cref="RefreshingAWSCredentials.PreemptExpiryTime"/>,
    /// with a minimum 5-second delay between refresh attempts.
    /// </para>
    /// </remarks>
    private class RefreshingCredentials : AWSCredentials
    {
        #region Private Fields

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The underlying AWS credentials.
        /// </summary>
        private readonly RefreshingAWSCredentials _refreshingAWSCredentials;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshingCredentials"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="refreshingAWSCredentials">The underlying AWS credentials to wrap.</param>
        public RefreshingCredentials(
            ILogger logger,
            RefreshingAWSCredentials refreshingAWSCredentials)
        {
            _logger = logger;
            _refreshingAWSCredentials = refreshingAWSCredentials;

            // Start the refresh loop in the background (fire and forget)
            _ = ScheduleRefreshCredentialsAsync();
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override ImmutableCredentials GetCredentials()
        {
            // Delegate to the underlying credentials
            return _refreshingAWSCredentials.GetCredentials();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Triggers a credential refresh and calculates when the next refresh should occur.
        /// </summary>
        /// <returns>The UTC time when the next credential refresh should be scheduled.</returns>
        /// <remarks>
        /// <para>
        /// Calls <c>GetCredentials()</c> on the underlying <see cref="RefreshingAWSCredentials"/>,
        /// which triggers the SDK's built-in refresh logic if within the preempt expiry window.
        /// </para>
        /// <para>
        /// Uses the public <see cref="RefreshingAWSCredentials.Expiration"/> property to calculate
        /// the next refresh time, ensuring a minimum 5-second delay between attempts.
        /// </para>
        /// </remarks>
        private DateTime RefreshCredentials()
        {
            // Trigger the SDK's refresh logic by calling GetCredentials
            // This will refresh if we're within the preempt expiry window
            _ = _refreshingAWSCredentials.GetCredentials();

            // Define minimum delay to avoid excessive refresh attempts
            var minRefreshOn = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            // Retrieve credential expiration from the public SDK property
            var expiration = _refreshingAWSCredentials.Expiration;
            if (expiration is null)
            {
                return minRefreshOn;
            }

            // Calculate next refresh time: expiration - preempt window
            var refreshOn = expiration.Value - _refreshingAWSCredentials.PreemptExpiryTime;

            // Log the calculated refresh time for monitoring
            _logger.LogInformation(
                "AWSCredentialsManager.RefreshingCredentials.RefreshCredentials: refreshOn = '{refreshOn:o}'.",
                refreshOn);

            // Apply minimum delay constraint
            return refreshOn >= minRefreshOn
                ? refreshOn
                : minRefreshOn;
        }

        /// <summary>
        /// Orchestrates the credential refresh cycle with timing and recursive scheduling.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Calls <see cref="RefreshCredentials"/> to trigger refresh and get next refresh time,
        /// waits for the calculated delay using <see cref="Task.Delay"/>, then recursively
        /// schedules the next cycle using fire-and-forget pattern.
        /// </para>
        /// <para>
        /// Logs timing information to monitor refresh performance and identify potential issues.
        /// </para>
        /// </remarks>
        private async Task ScheduleRefreshCredentialsAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Log the start of refresh cycle
            _logger.LogInformation(
                "AWSCredentialsManager.RefreshingCredentials.ScheduleRefreshCredentialsAsync");

            // Perform credential refresh and get next scheduled refresh time
            var refreshOn = RefreshCredentials();

            stopwatch.Stop();
            _logger.LogInformation(
                "AWSCredentialsManager.RefreshingCredentials.ScheduleRefreshCredentialsAsync: elapsedMilliseconds = {elapsedMilliseconds} ms.",
                stopwatch.ElapsedMilliseconds);

            // Calculate delay until next refresh
            var dueTime = refreshOn - DateTime.UtcNow;
            await Task.Delay(dueTime);

            // Schedule next refresh cycle (fire and forget)
            _ = ScheduleRefreshCredentialsAsync();
        }

        #endregion
    }

    #endregion
}
