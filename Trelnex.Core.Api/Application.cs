using System.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.CommandProviders;
using Trelnex.Core.Api.Configuration;
using Trelnex.Core.Api.Exceptions;
using Trelnex.Core.Api.HealthChecks;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Api.Observability;
using Trelnex.Core.Api.Rewrite;
using Trelnex.Core.Api.Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Trelnex.Core.Api;

/// <summary>
/// Configures and runs a web application with standardized middleware and services.
/// </summary>
public static class Application
{
    #region Public Static Methods

    /// <summary>
    /// Configures and runs an ASP.NET Core web application.
    /// </summary>
    /// <param name="args">Command line arguments passed to the application.</param>
    /// <param name="addApplication">Delegate to register application-specific services.</param>
    /// <param name="useApplication">Delegate to configure application-specific endpoints and middleware.</param>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the required ServiceConfiguration section is missing.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when authentication is not properly configured.
    /// </exception>
    public static void Run(
        string[] args,
        Action<IServiceCollection, IConfiguration, ILogger> addApplication,
        Action<WebApplication> useApplication)
    {
        // Create the web application using the standardized configuration.
        var app = CreateWebApplication(
            args,
            addApplication,
            useApplication);

        // Run the application.
        app.Run();
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Creates and configures an ASP.NET Core web application.
    /// </summary>
    /// <param name="args">Command line arguments passed to the application.</param>
    /// <param name="addApplication">Delegate to register application-specific services.</param>
    /// <param name="useApplication">Delegate to configure application-specific endpoints and middleware.</param>
    /// <returns>A configured <see cref="WebApplication"/> instance ready to run.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the required ServiceConfiguration section is missing.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when authentication is not properly configured.
    /// </exception>
    internal static WebApplication CreateWebApplication(
        string[] args,
        Action<IServiceCollection, IConfiguration, ILogger> addApplication,
        Action<WebApplication> useApplication)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add the configuration.
        builder.AddConfiguration();

        // Get the service configuration.
        var serviceConfiguration = builder.Configuration
            .GetSection("ServiceConfiguration")
            .Get<ServiceConfiguration>()
            ?? throw new ConfigurationErrorsException("The service configuration is not found.");

        builder.Services.AddSingleton(serviceConfiguration);

        // Add prometheus metrics server, http client metrics and open telemetry.
        builder.Services.AddObservability(builder.Configuration, serviceConfiguration);

        // Add serilog for structured logging.
        var bootstrapLogger = builder.Services.AddSerilog(
            builder.Configuration,
            serviceConfiguration);

        // Configure global exception handling.
        builder.Services.AddExceptionHandler<HttpStatusCodeExceptionHandler>();

        // Disable automatic 400 responses for more control over API behavior.
        // See: https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2#disable-automatic-400-response-3
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressConsumesConstraintForFormFileParameters = true;
            options.SuppressInferBindingSourcesForParameters = true;
            options.SuppressMapClientErrors = true;
            options.SuppressModelStateInvalidFilter = true;
        });

        // Configure forwarded headers to handle proxy scenarios.
        // Ensures that callback URLs use the correct protocol (https).
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // Inject our security provider.
        var securityProvider = new SecurityProvider();
        builder.Services.AddSingleton<ISecurityProvider>(securityProvider);

        // Register application-specific services.
        addApplication(builder.Services, builder.Configuration, bootstrapLogger);

        // Add health check services.
        builder.Services.AddIdentityHealthChecks();
        builder.Services.AddCommandProviderHealthChecks();
        builder.Services.AddDefaultHealthChecks();

        var app = builder.Build();

        // Add exception handler middleware.
        // See: https://github.com/dotnet/aspnetcore/issues/51888
        app.UseExceptionHandler(_ => { });

        // Add Serilog request logging.
        // See: https://github.com/serilog/serilog-aspnetcore?tab=readme-ov-file#request-logging
        app.UseSerilogRequestLogging();

        // Configure URL rewriting rules.
        app.UseRewriteRules();

        // Add forwarded headers middleware (enables proper handling behind proxies).
        app.UseForwardedHeaders();

        // Enable health check endpoints.
        app.MapHealthChecks();

        // Configure observability middleware (metrics, tracing).
        app.UseObservability();

        // Configure standard HTTP pipeline middleware.
        app.UseHttpsRedirection();

        // Check if authentication is configured.
        if (app.Services.GetService<IAuthenticationSchemeProvider>() is not null)
        {
            app.UseAuthentication();

            // Check if authorization is configured.
            if (app.Services.GetService<IAuthorizationService>() is not null)
            {
                app.UseAuthorization();
            }
        }

        // Configure application-specific endpoints and middleware.
        useApplication(app);

        return app;
    }

    #endregion
}
