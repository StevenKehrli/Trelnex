using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Swagger document filter that registers security definitions.
/// </summary>
/// <remarks>
/// Adds security scheme definitions to the OpenAPI document based on configured security providers.
/// </remarks>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/> that provides security definitions.</param>
internal class SecurityFilter(
    ISecurityProvider securityProvider) : IDocumentFilter
{
    #region Public Methods

    /// <summary>
    /// Applies security definitions to the OpenAPI document.
    /// </summary>
    /// <param name="document">The OpenAPI document being generated.</param>
    /// <param name="context">The document filter context.</param>
    public void Apply(
        OpenApiDocument document,
        DocumentFilterContext context)
    {
        // Get the security definitions and add to this document.
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
                Scheme = "bearer"
            };

            // Add the security scheme to the document's components.
            document.Components.SecuritySchemes.Add(securityDefinition.JwtBearerScheme, openApiSecurityScheme);
        }
    }

    #endregion
}
