using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Trelnex.Core.Api.Swagger;

/// <summary>
/// Document filter that removes the tags collection from the OpenAPI document.
/// </summary>
internal class RemoveTagsFilter : IDocumentFilter
{
    /// <summary>
    /// Removes the tags collection from the OpenAPI document.
    /// </summary>
    /// <param name="swaggerDoc">The OpenAPI document being generated.</param>
    /// <param name="context">The document filter context.</param>
    public void Apply(
        OpenApiDocument swaggerDoc,
        DocumentFilterContext context)
    {
        swaggerDoc.Tags = null;
    }
}
