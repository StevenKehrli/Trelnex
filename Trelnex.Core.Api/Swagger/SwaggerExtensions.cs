using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using Trelnex.Core.Api.Configuration;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Provides extension methods for configuring Swagger/OpenAPI.
/// </summary>
/// <remarks>
/// These extensions simplify the setup of Swagger documentation.
/// </remarks>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger generator and explorer services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Swagger services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ServiceConfiguration"/> is not registered.</exception>
    public static IServiceCollection AddSwaggerToServices(
        this IServiceCollection services)
    {
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // Format the version string.
        var versionString = FormatVersionString(serviceConfiguration.SemVersion);

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(versionString, new()
            {
                Title = serviceConfiguration.DisplayName,
                Version = serviceConfiguration.Version,
                Description = serviceConfiguration.Description,
            });

            options.EnableAnnotations(
                enableAnnotationsForInheritance: true,
                enableAnnotationsForPolymorphism: true);

            // Define a local function to determine the order of HTTP methods.
            static int GetHttpMethodOrdinal(string httpMethod)
            {
                return httpMethod switch
                {
                    "GET" => 00,
                    "POST" => 01,
                    "PUT" => 02,
                    "PATCH" => 03,
                    "DELETE" => 04,
                    _ => 99, // Place unknown methods last.
                };
            }

            options.OrderActionsBy((apiDesc) =>
            {
                // Get the ordinal value for the HTTP method.
                var httpMethodOrdinal = GetHttpMethodOrdinal(apiDesc.HttpMethod ?? string.Empty);

                // Order by relative path and then by HTTP method ordinal.
                return $"{apiDesc.RelativePath} {httpMethodOrdinal}";
            });

            options.SchemaFilter<SchemaFilter>();
            options.OperationFilter<AuthorizeFilter>();
            options.DocumentFilter<SecurityFilter>();
        });

        return services;
    }

    /// <summary>
    /// Configures a <see cref="WebApplication"/> to use Swagger UI and JSON endpoints.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure with Swagger.</param>
    /// <returns>The <see cref="WebApplication"/> for method chaining.</returns>
    public static WebApplication AddSwaggerToWebApplication(
        this WebApplication app)
    {
        var serviceConfiguration = app.Services.GetRequiredService<ServiceConfiguration>();

        // Format the version string.
        var versionString = FormatVersionString(serviceConfiguration.SemVersion);

        // Allow CORS for Swagger UI.
        app.Use((context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                context.Response.Headers.AccessControlAllowOrigin = "*";
            }

            return next.Invoke();
        });

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{versionString}/swagger.json", serviceConfiguration.DisplayName);
        });

        return app;
    }

    /// <summary>
    /// Formats a semantic version into a Swagger version string.
    /// </summary>
    /// <param name="semVer">The semantic version to format.</param>
    /// <returns>A formatted version string in the format "v{Major}".</returns>
    private static string FormatVersionString(
        SemVersion semVer)
    {
        // Format the version string.
        return $"v{semVer.Major}";
    }
}
