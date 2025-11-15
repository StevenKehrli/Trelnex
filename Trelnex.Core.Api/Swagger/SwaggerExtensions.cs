using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Semver;
using Swashbuckle.AspNetCore.SwaggerGen;
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
    #region Public Static Methods

    /// <summary>
    /// Adds Swagger generator and explorer services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Swagger services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ServiceConfiguration"/> is not registered.</exception>
    public static IServiceCollection AddSwaggerToServices(
        this IServiceCollection services)
    {
        var serviceDescriptor = services.FirstOrDefault(serviceDescriptor => serviceDescriptor.ServiceType == typeof(ServiceConfiguration))
            ?? throw new InvalidOperationException("ServiceConfiguration is not registered.");

        var serviceConfiguration = (serviceDescriptor.ImplementationInstance as ServiceConfiguration)!;

        // Format the version string.
        var versionString = FormatVersionString(serviceConfiguration.SemVersion);

        services.AddEndpointsApiExplorer();

        // Register the configuration class that will add security definitions.
        // This is called by the options framework before SwaggerGen is fully configured.
        services.ConfigureOptions<ConfigureSwaggerGenOptions>();

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
                    "HEAD" => 05,
                    _ => 99, // Place unknown methods last.
                };
            }

            options.OrderActionsBy(apiDescription =>
            {
                // Get the ordinal value for the HTTP method.
                var httpMethodOrdinal = GetHttpMethodOrdinal(apiDescription.HttpMethod ?? string.Empty);

                // Order by relative path and then by HTTP method ordinal.
                return $"{apiDescription.RelativePath} {httpMethodOrdinal}";
            });

            options.SchemaFilter<SchemaFilter>();
            options.OperationFilter<AuthorizeFilter>();
            options.DocumentFilter<RemoveTagsFilter>();
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
            options.ConfigObject.AdditionalItems.Add("tagsSorter", "alpha");
            options.SwaggerEndpoint($"/swagger/{versionString}/swagger.json", serviceConfiguration.DisplayName);
        });

        return app;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Formats a semantic version into a Swagger version string.
    /// </summary>
    /// <param name="semanticVersion">The semantic version to format.</param>
    /// <returns>A formatted version string in the format "v{Major}".</returns>
    private static string FormatVersionString(
        SemVersion semanticVersion)
    {
        // Format the version string.
        return $"v{semanticVersion.Major}";
    }

    #endregion
}

/// <summary>
/// Configures SwaggerGen options with security definitions from the security provider.
/// </summary>
/// <remarks>
/// This class is called by the options framework to configure SwaggerGen before filters run.
/// </remarks>
/// <param name="securityProvider">The security provider that supplies authentication schemes.</param>
internal class ConfigureSwaggerGenOptions(
    Authentication.ISecurityProvider securityProvider) : IConfigureOptions<SwaggerGenOptions>
{
    /// <summary>
    /// Configures SwaggerGen options by registering security definitions.
    /// </summary>
    /// <param name="options">The SwaggerGen options to configure.</param>
    public void Configure(SwaggerGenOptions options)
    {
        // Register security definitions from the security provider.
        // This must happen before the OperationFilter runs so that security scheme references can be resolved.
        var securityDefinitions = securityProvider.GetSecurityDefinitions();

        foreach (var securityDefinition in securityDefinitions)
        {
            options.AddSecurityDefinition(securityDefinition.JwtBearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
                Description = $"Authorization Header JWT Bearer Token; Audience {securityDefinition.Audience}; Scope {securityDefinition.Scope}",
                Name = "Authorization",
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
        }
    }
}
