using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;
using Trelnex.Core.Api.Configuration;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Trelnex.Core.Api.Serilog;

/// <summary>
/// Provides extension methods for configuring structured logging with Serilog.
/// </summary>
/// <remarks>
/// Configures a standardized logging setup using Serilog.
/// </remarks>
public static class SerilogExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Configures Serilog logging for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="serviceConfiguration">The service identification details used for logging.</param>
    /// <returns>A bootstrap logger for application startup logging.</returns>
    public static ILogger AddSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceConfiguration serviceConfiguration)
    {
        // Create a bootstrap logger for early application startup logging.
        Log.Logger = new LoggerConfiguration()
            .ConfigureLogger(serviceConfiguration)
            .CreateBootstrapLogger();

        // Configure the main application logger.
        services.AddSerilog((serviceProvider, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(serviceProvider)
                .ConfigureLogger(serviceConfiguration);
        });

        // Return a Microsoft.Extensions.Logging adapter for the bootstrap logger.
        return new SerilogLoggerFactory(Log.Logger).CreateLogger(serviceConfiguration.FullName);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Applies standard configuration to a Serilog logger configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration to modify.</param>
    /// <param name="serviceConfiguration">The service identification details.</param>
    /// <returns>The configured logger configuration for method chaining.</returns>
    private static LoggerConfiguration ConfigureLogger(
        this LoggerConfiguration loggerConfiguration,
        ServiceConfiguration serviceConfiguration)
    {
        // Use compact JSON format for structured logging.
        var formatter = new RenderedCompactJsonFormatter();

        return loggerConfiguration
            // Enrich logs with contextual information.
            .Enrich.FromLogContext()
            .Enrich.WithSpan()

            // Configure standard outputs.
            .WriteTo.Console(formatter)
            .WriteTo.Debug(formatter)

            // Configure OpenTelemetry integration.
            .WriteTo.OpenTelemetry(telemetryOptions =>
            {
                // Add service identification to logs.
                telemetryOptions.ResourceAttributes.Add("service.name", serviceConfiguration.FullName);
                telemetryOptions.ResourceAttributes.Add("service.version", serviceConfiguration.Version);
            });
    }

    #endregion
}
