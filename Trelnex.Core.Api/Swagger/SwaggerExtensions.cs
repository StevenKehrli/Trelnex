using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Extension methods to add Swagger to the <see cref="IServiceCollection"/> and the <see cref="WebApplication"/>.
/// </summary>
public static class SwaggerExtensions
{
    /// <summary>
    /// Add Swagger to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSwaggerToServices(
        this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
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
    /// Add the Swagger into the <see cref="WebApplication"/>.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the Swagger endpoints to.</param>
    /// <returns>The <see cref="WebApplication"/>.</returns>
    public static WebApplication AddSwaggerToWebApplication(
        this WebApplication app)
    {
        var swaggerOptions = app.Configuration.GetSection("Swagger").Get<SwaggerConfiguration>();

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
            options.SwaggerEndpoint("/swagger/v1/swagger.json", swaggerOptions?.ServiceName);
        });

        return app;
    }

    /// <summary>
    /// Represents the configuration properties for Swagger.
    /// </summary>
    /// <param name="ServiceName">The service name that appears in the document selector drop-down.</param>
    private record SwaggerConfiguration(
        string? ServiceName
    );
}
