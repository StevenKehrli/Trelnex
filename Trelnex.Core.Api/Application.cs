using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Context;
using Trelnex.Core.Api.Exceptions;
using Trelnex.Core.Api.HealthChecks;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Observability;
using Trelnex.Core.Api.Rewrite;
using Trelnex.Core.Api.Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Trelnex.Core.Api;

public static class Application
{
    /// <summary>
    /// Configures and run the HTTP pipeline and routes.
    /// </summary>
    /// <param name="args">The command line arguments</param>
    /// <param name="addApplication">The delegate to add the calling application to the <see cref="IServiceCollection"/>.</param>
    /// <param name="useApplication">The delegate to use the calling application into the <param name="IEndpointRouteBuilder">.</param></param>
    /// <param name="addHealthChecks">An optional delegate to add additional health checks to the <see cref="IServiceCollection"/>.</param>
    public static void Run(
        string[] args,
        Action<IServiceCollection, IConfiguration, ILogger> addApplication,
        Action<WebApplication> useApplication,
        Action<IHealthChecksBuilder, IConfiguration>? addHealthChecks = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        // add serilog
        var bootstrapLogger = builder.Services.AddSerilog(
            builder.Configuration,
            nameof(Application));

        // add the configuration
        builder.AddConfiguration();

        // handle our exceptions
        builder.Services.AddExceptionHandler<HttpStatusCodeExceptionHandler>();

        // https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2#disable-automatic-400-response-3
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressConsumesConstraintForFormFileParameters = true;
            options.SuppressInferBindingSourcesForParameters = true;
            options.SuppressMapClientErrors = true;
            options.SuppressModelStateInvalidFilter = true;
        });

        // we need the forwarded headers (proto = https)
        // so any callback paths will be correct (proto = https)
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // add the request context as a transient object
        builder.Services.AddRequestContext();

        // add the calling application
        addApplication(builder.Services, builder.Configuration, bootstrapLogger);

        // validate authentication was configured
        builder.Services.ThrowIfAuthenticationNotAdded();

        // add prometheus metrics server and http client metrics and open telemetry
        builder.Services.AddObservability(builder.Configuration);

        // add the health checks
        builder.Services.AddIdentityHealthChecks();
        builder.Services.AddCommandProviderHealthChecks();
        builder.Services.AddHealthChecks(healthChecksBuilder =>
        {
            // the calling application health checks
            addHealthChecks?.Invoke(healthChecksBuilder, builder.Configuration);
        });

        var app = builder.Build();

        // https://github.com/dotnet/aspnetcore/issues/51888
        app.UseExceptionHandler(_ => { });

        // serilog request logging
        // https://github.com/serilog/serilog-aspnetcore?tab=readme-ov-file#request-logging
        app.UseSerilogRequestLogging();

        // add the rewrite rules
        app.UseRewriteRules();

        // use the forwarded headers (see above)
        app.UseForwardedHeaders();

        // map health checks
        app.MapHealthChecks();

        // add http metrics
        app.UseObservability();

        // configure the http request pipeline
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        // use the calling application
        useApplication(app);

        app.Run();
    }
}
