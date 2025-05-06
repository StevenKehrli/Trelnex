using Microsoft.AspNetCore.Builder;
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
/// These extensions configure a standardized logging setup using Serilog,
/// with structured logging format (JSON), console output, and OpenTelemetry integration.
/// The configuration follows Serilog's recommended two-stage initialization pattern.
/// </remarks>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures Serilog logging for the application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="serviceConfiguration">The service identification details used for logging.</param>
    /// <returns>A bootstrap logger for application startup logging.</returns>
    /// <remarks>
    /// This method:
    /// <list type="number">
    ///   <item>Creates a bootstrap logger for early application startup</item>
    ///   <item>Configures the main application logger with settings from configuration</item>
    ///   <item>Sets up standardized log enrichment and formatting</item>
    ///   <item>Enables OpenTelemetry integration for logs</item>
    /// </list>
    ///
    /// This implementation follows the recommended two-stage initialization pattern
    /// as described in the Serilog documentation:
    /// https://github.com/serilog/serilog-aspnetcore?tab=readme-ov-file#two-stage-initialization
    /// </remarks>
    public static ILogger AddSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceConfiguration serviceConfiguration)
    {
        // Create a bootstrap logger for early application startup logging
        Log.Logger = new LoggerConfiguration()
            .ConfigureLogger(serviceConfiguration)
            .CreateBootstrapLogger();

        // Configure the main application logger
        services.AddSerilog((serviceProvider, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(configuration)   // Use settings from configuration
                .ReadFrom.Services(serviceProvider)      // Use services from DI container
                .ConfigureLogger(serviceConfiguration);  // Apply standard configuration
        });

        // Return a Microsoft.Extensions.Logging adapter for the bootstrap logger
        return new SerilogLoggerFactory(Log.Logger).CreateLogger(serviceConfiguration.FullName);
    }

    /// <summary>
    /// Applies standard configuration to a Serilog logger configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The logger configuration to modify.</param>
    /// <param name="serviceConfiguration">The service identification details.</param>
    /// <returns>The configured logger configuration for method chaining.</returns>
    /// <remarks>
    /// This method configures:
    /// <list type="bullet">
    ///   <item>Log enrichment with context properties and span information</item>
    ///   <item>Console output with compact JSON formatting</item>
    ///   <item>Debug output for development scenarios</item>
    ///   <item>OpenTelemetry integration with service identification</item>
    /// </list>
    ///
    /// The compact JSON format ensures logs are structured for easy filtering
    /// and analysis in log aggregation systems, while also being human-readable.
    /// </remarks>
    private static LoggerConfiguration ConfigureLogger(
        this LoggerConfiguration loggerConfiguration,
        ServiceConfiguration serviceConfiguration)
    {
        // Use compact JSON format for structured logging
        var formatter = new RenderedCompactJsonFormatter();

        return loggerConfiguration
            // Enrich logs with contextual information
            .Enrich.FromLogContext()                     // Add properties from LogContext
            .Enrich.WithSpan()                           // Add trace/span IDs from OpenTelemetry

            // Configure standard outputs
            .WriteTo.Console(formatter)                  // Write to console with JSON formatting
            .WriteTo.Debug(formatter)                    // Write to debug output in development

            // Configure OpenTelemetry integration
            .WriteTo.OpenTelemetry(options =>
            {
                // Add service identification to logs
                options.ResourceAttributes.Add("service.name", serviceConfiguration.FullName);
                options.ResourceAttributes.Add("service.version", serviceConfiguration.Version);
            });
    }
}
