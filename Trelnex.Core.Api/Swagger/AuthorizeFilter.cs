using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Swagger operation filter that adds security requirements based on authorization attributes.
/// </summary>
/// <remarks>
/// Translates <see cref="AuthorizeAttribute"/> instances into OpenAPI security requirements.
/// </remarks>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/> used to resolve security requirements.</param>
internal class AuthorizeFilter(
    ISecurityProvider securityProvider) : IOperationFilter
{
    #region Public Methods

    /// <summary>
    /// Applies security requirements to an OpenAPI operation.
    /// </summary>
    /// <param name="operation">The OpenAPI operation being documented.</param>
    /// <param name="context">The operation filter context.</param>
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        // Initialize the security mechanisms for this operation.
        operation.Security = [];

        // Get any authorize attributes on the endpoint.
        var authorizeAttributes = context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>();

        foreach (var authorizeAttribute in authorizeAttributes)
        {
            // Get the security requirement specified by the authorize attribute.
            var securityRequirement = securityProvider.GetSecurityRequirement(authorizeAttribute.Policy!);

            // Create the security requirement and add to this operation.
            var openApiSecurityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = securityRequirement.JwtBearerScheme
                        }
                    },
                    securityRequirement.RequiredRoles
                        .Select(requiredRole => $"{securityRequirement.Audience}/{securityRequirement.Scope}/{requiredRole}")
                        .ToArray()
                }
            };

            operation.Security.Add(openApiSecurityRequirement);
        }
    }

    #endregion
}
