using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Compact;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Trelnex.Core.Api.Serilog;

/// <summary>
/// Extension methods to add Serilog to the <see cref="WebApplicationBuilder"/>.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>
    /// Add the configuration to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <param name="bootstrapCategoryName">The category name for messages produced by the bootstrap logger.</param>
    /// <returns>A bootstrap <see cref="Serilog.ILogger"/></returns>
    public static ILogger AddSerilog(
        this IServiceCollection services,
        IConfiguration configuration,
        string bootstrapCategoryName)
    {
        // add serilog
        // https://github.com/serilog/serilog-aspnetcore?tab=readme-ov-file#two-stage-initialization

        var formatter = new RenderedCompactJsonFormatter();

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .WriteTo.Console(formatter)
            .WriteTo.Debug(formatter)
            .CreateBootstrapLogger();

        services.AddSerilog((services, configureLogger) =>
        {
            configureLogger
                .ReadFrom.Configuration(configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .WriteTo.Console(formatter)
                .WriteTo.Debug(formatter);
        });

        return new SerilogLoggerFactory(Log.Logger).CreateLogger(bootstrapCategoryName);
    }
}
