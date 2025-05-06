using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Swagger document filter that registers security definitions in the OpenAPI specification.
/// </summary>
/// <remarks>
/// This filter adds security scheme definitions to the OpenAPI document based on the configured
/// security providers. These definitions describe the authentication mechanisms that the API supports,
/// including JWT bearer tokens with their associated audiences and scopes.
///
/// Security definitions are essential for the Swagger UI to properly render authentication
/// requirements and provide interactive authentication experiences.
/// </remarks>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/> that provides security definitions to register in the OpenAPI document.</param>
internal class SecurityFilter(
    ISecurityProvider securityProvider) : IDocumentFilter
{
    /// <summary>
    /// Applies security definitions to the OpenAPI document.
    /// </summary>
    /// <param name="document">The OpenAPI document being generated.</param>
    /// <param name="context">The document filter context.</param>
    /// <remarks>
    /// For each security definition provided by the <see cref="ISecurityProvider"/>:
    /// <list type="bullet">
    ///   <item>Creates an OpenAPI security scheme configured for JWT bearer authentication</item>
    ///   <item>Sets appropriate header location, description, and format information</item>
    ///   <item>Adds the security scheme to the document's components section</item>
    /// </list>
    ///
    /// The descriptions include the audience and scope information to help developers
    /// understand what JWT tokens are required for different parts of the API.
    /// </remarks>
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
