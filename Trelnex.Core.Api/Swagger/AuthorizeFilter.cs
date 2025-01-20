using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Initializes a new instance of the <see cref="AuthorizeFilter"/>.
/// </summary>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/>, from Dependency Injection, to get the security requirement specified by the <see cref="AuthorizeAttribute"/>.</param>
internal class AuthorizeFilter(
    ISecurityProvider securityProvider) : IOperationFilter
{
    /// <summary>
    /// Apply this filter to the specified <see cref="OpenApiOperation"/> and <see cref="OperationFilterContext"/>.
    /// </summary>
    /// <param name="operation">The specified <see cref="OpenApiOperation"/>.</param>
    /// <param name="context">The specified <see cref="OperationFilterContext"/>.</param>
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        // initialize the security mechanisms for this operation
        operation.Security = [];

        // get any authorize attributes on the endpoint
        var authorizeAttributes =
            context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>();

        foreach (var authorizeAttribute in authorizeAttributes)
        {
            // get the security requirement specified by the authorize attribute
            var securityRequirement = securityProvider.GetSecurityRequirement(authorizeAttribute.Policy!);

            // create the security requirement and add to this operation
            var openApiSecurityRequirement = new OpenApiSecurityRequirement()
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
                    securityRequirement
                        .RequiredRoles
                        .Select(rr => $"{securityRequirement.Audience}/{securityRequirement.Scope}/{rr}")
                        .ToArray()
                }
            };

            operation.Security.Add(openApiSecurityRequirement);
        }
    }
}
