using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using Trelnex.Core.Api.Configuration;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Provides extension methods for configuring Swagger/OpenAPI documentation in ASP.NET Core applications.
/// </summary>
/// <remarks>
/// These extensions simplify the setup of Swagger documentation by automatically configuring
/// the service with information from <see cref="ServiceConfiguration"/>, applying consistent
/// security requirements, and ordering API endpoints in a predictable way.
///
/// The implementation supports:
/// <list type="bullet">
///   <item>Automatic version extraction from semantic versioning</item>
///   <item>Security definition integration with authentication mechanisms</item>
///   <item>Consistent API endpoint ordering by HTTP method and path</item>
///   <item>Authorization requirement documentation through operation filters</item>
/// </list>
/// </remarks>
public static class SwaggerExtensions
{
    /// <summary>
    /// Adds Swagger generator and explorer services to the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Swagger services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ServiceConfiguration"/> is not registered in the service collection.</exception>
    /// <remarks>
    /// This method configures Swagger with the following features:
    /// <list type="bullet">
    ///   <item>Uses service information (title, version, description) from <see cref="ServiceConfiguration"/></item>
    ///   <item>Enables annotation support for inheritance and polymorphism</item>
    ///   <item>Orders API endpoints by path and HTTP method (GET, POST, PUT, PATCH, DELETE)</item>
    ///   <item>Applies security requirements based on authorization attributes</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddSwaggerToServices(
        this IServiceCollection services)
    {
        var serviceDescriptor = services
            .FirstOrDefault(sd => sd.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // format the version string
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

            static int GetHttpMethodOrdinal(string httpMethod)
            {
                return httpMethod switch
                {
                    "GET" => 00,
                    "POST" => 01,
                    "PUT" => 02,
                    "PATCH" => 03,
                    "DELETE" => 04,
                    _ => 99,
                };
            }

            options.OrderActionsBy((apiDesc) =>
            {
                var httpMethodOrdinal = GetHttpMethodOrdinal(apiDesc.HttpMethod ?? string.Empty);

                return $"{apiDesc.RelativePath} {httpMethodOrdinal}";
            });

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
    /// <remarks>
    /// This method performs the following configuration:
    /// <list type="bullet">
    ///   <item>Enables CORS for the Swagger UI by setting Access-Control-Allow-Origin header</item>
    ///   <item>Registers the Swagger JSON endpoint with versioning</item>
    ///   <item>Configures the Swagger UI with the service display name</item>
    /// </list>
    ///
    /// This method should be called in the application configuration pipeline after
    /// <see cref="AddSwaggerToServices(IServiceCollection)"/> has been called during service registration.
    /// </remarks>
    public static WebApplication AddSwaggerToWebApplication(
        this WebApplication app)
    {
        var serviceConfiguration = app.Services.GetRequiredService<ServiceConfiguration>();

        // format the version string
        var versionString = FormatVersionString(serviceConfiguration.SemVersion);

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
    /// <remarks>
    /// This method creates a simplified version string using only the major version number,
    /// which is the recommended practice for Swagger/OpenAPI version identifiers.
    /// </remarks>
    private static string FormatVersionString(
        SemVersion semVer)
    {
        // format the version string
        return $"v{semVer.Major}";
    }
}
