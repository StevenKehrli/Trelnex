using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Trelnex.Core.Amazon.Observability;

/// <summary>
/// Extension methods for adding AWS instrumentation to OpenTelemetry.
/// </summary>
/// <remarks>
/// Integrates with AWS X-Ray and other AWS monitoring services.
/// </remarks>
internal static class ObservabilityExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds AWS instrumentation to the OpenTelemetry tracer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    /// <remarks>
    /// Configures OpenTelemetry to send traces to AWS X-Ray.
    /// Used by <see cref="Identity.AmazonIdentityExtensions.AddAmazonIdentity"/>.
    /// </remarks>
    public static IServiceCollection AddAWSInstrumentation(
        this IServiceCollection services)
    {
        // Configure OpenTelemetry to include AWS instrumentation
        services.ConfigureOpenTelemetryTracerProvider(configure =>
        {
            // Add the AWS instrumentation to trace all AWS SDK calls
            configure.AddAWSInstrumentation();
        });

        // Return the service collection to allow for method chaining
        return services;
    }

    #endregion
}
