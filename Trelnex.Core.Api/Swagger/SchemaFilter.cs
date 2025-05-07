using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Trelnex.Core.Api.Swagger;

internal class SchemaFilter : ISchemaFilter
{
    public void Apply(
        OpenApiSchema schema,
        SchemaFilterContext context)
    {
        schema.AdditionalPropertiesAllowed = false;
    }
}
