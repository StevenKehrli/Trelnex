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
/// Extension methods to add Serilog to the <see cref="WebApplicationBuilder"/>.
/// </summary>
internal static class SerilogExtensions
{
    /// <summary>
    /// Add the configuration to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    /// <param name="serviceConfiguration">The <see cref="ServiceConfiguration"/>.</param>
    /// <returns>A bootstrap <see cref="Serilog.ILogger"/></returns>
    public static ILogger AddSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        ServiceConfiguration serviceConfiguration)
    {
        // add serilog
        // https://github.com/serilog/serilog-aspnetcore?tab=readme-ov-file#two-stage-initialization

        Log.Logger = new LoggerConfiguration()
            .ConfigureLogger(serviceConfiguration)
            .CreateBootstrapLogger();

        services.AddSerilog((services, configureLogger) =>
        {
            configureLogger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .ConfigureLogger(serviceConfiguration);
        });

        return new SerilogLoggerFactory(Log.Logger).CreateLogger(serviceConfiguration.Name);
    }

    /// <summary>
    /// Configure the logger.
    /// </summary>
    /// <param name="loggerConfiguration">The <see cref="LoggerConfiguration"/>.</param>
    /// <param name="serviceConfiguration">The <see cref="ServiceConfiguration"/>.</param>
    /// <returns>The <see cref="LoggerConfiguration"/>.</returns>
    private static LoggerConfiguration ConfigureLogger(
        this LoggerConfiguration loggerConfiguration,
        ServiceConfiguration serviceConfiguration)
    {
        var formatter = new RenderedCompactJsonFormatter();

        return loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console(formatter)
            .WriteTo.Debug(formatter)
            .WriteTo.OpenTelemetry(options =>
            {
                options.ResourceAttributes.Add("service.name", serviceConfiguration.Name);
                options.ResourceAttributes.Add("service.version", serviceConfiguration.Version);
            });
    }
}
