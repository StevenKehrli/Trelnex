using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Swagger operation filter that adds security requirements to API operations based on authorization attributes.
/// </summary>
/// <remarks>
/// This filter examines <see cref="AuthorizeAttribute"/> instances on API endpoints and
/// translates them into OpenAPI security requirements. This ensures that the Swagger UI
/// properly represents which operations require authentication and what scopes/roles are needed.
///
/// The filter works by:
/// <list type="number">
///   <item>Identifying endpoints with <see cref="AuthorizeAttribute"/> applied</item>
///   <item>Looking up the corresponding security requirements from the <see cref="ISecurityProvider"/></item>
///   <item>Adding appropriate security schemes and scopes to the OpenAPI documentation</item>
/// </list>
/// </remarks>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/> used to resolve security requirements from policy names.</param>
internal class AuthorizeFilter(
    ISecurityProvider securityProvider) : IOperationFilter
{
    /// <summary>
    /// Applies security requirements to an OpenAPI operation based on authorization attributes.
    /// </summary>
    /// <param name="operation">The OpenAPI operation being documented.</param>
    /// <param name="context">The operation filter context containing metadata about the API endpoint.</param>
    /// <remarks>
    /// For each <see cref="AuthorizeAttribute"/> on the endpoint:
    /// <list type="bullet">
    ///   <item>Gets the corresponding security requirement from the policy name</item>
    ///   <item>Creates an OpenAPI security requirement with appropriate scheme and scopes</item>
    ///   <item>Adds the security requirement to the operation's security collection</item>
    /// </list>
    ///
    /// Security scopes are formatted as "{audience}/{scope}/{role}" to provide a clear
    /// representation of the required permissions in the Swagger UI.
    /// </remarks>
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
