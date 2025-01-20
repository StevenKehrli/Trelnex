using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Initializes a new instance of the <see cref="SecurityFilter"/>.
/// </summary>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/>, from Dependency Injection, to get the security definitions.</param>
internal class SecurityFilter(
    ISecurityProvider securityProvider) : IDocumentFilter
{
    /// <summary>
    /// Apply this filter to the specified <see cref="OpenApiOperation"/> and <see cref="OperationFilterContext"/>.
    /// </summary>
    /// <param name="document">The specified <see cref="OpenApiDocument"/>.</param>
    /// <param name="context">The specified <see cref="DocumentFilterContext"/>.</param>
    public void Apply(
        OpenApiDocument document,
        DocumentFilterContext context)
    {
        // get the security definitions and add to this document
        var securityDefinitions = securityProvider.GetSecurityDefinitions();

        foreach (var securityDefinition in securityDefinitions)
        {
            var openApiSecurityScheme = new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = $"Authorization Header JWT Bearer Token; Audience {securityDefinition.Audience}; Scope {securityDefinition.Scope}",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer",
            };

            document.Components.SecuritySchemes.Add(securityDefinition.JwtBearerScheme, openApiSecurityScheme);
        }
    }
}
