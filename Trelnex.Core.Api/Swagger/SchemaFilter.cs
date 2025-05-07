using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Swagger schema filter that controls schema properties.
/// </summary>
/// <remarks>
/// Customizes schema generation for all types in the API.
/// </remarks>
internal class SchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Applies schema customizations to the OpenAPI schema for a specific type.
    /// </summary>
    /// <param name="schema">The OpenAPI schema being modified.</param>
    /// <param name="context">The schema filter context.</param>
    public void Apply(
        OpenApiSchema schema,
        SchemaFilterContext context)
    {
        // Disable additional properties for strict schema validation.
        schema.AdditionalPropertiesAllowed = false;
    }
}