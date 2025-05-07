using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.Context;

/// <summary>
/// Provides extension methods for configuring and accessing request context data.
/// </summary>
/// <remarks>
/// Makes HTTP request information accessible throughout the application.
/// </remarks>
public static class RequestContextExtensions
{
    /// <summary>
    /// Registers the request context service with dependency injection.
    /// </summary>
    /// <param name="services">The service collection to add the context to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRequestContext(
        this IServiceCollection services)
    {
        services
            .AddScoped(provider =>
            {
                // Access the HTTP context through the accessor service.
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

                // Create a request context from the current HTTP context.
                return GetRequestContext(httpContextAccessor);
            });

        return services;
    }

    /// <summary>
    /// Creates a request context instance from the current HTTP context.
    /// </summary>
    /// <param name="httpContextAccessor">The accessor for the current HTTP context.</param>
    /// <returns>A populated request context object.</returns>
    private static IRequestContext GetRequestContext(
        IHttpContextAccessor? httpContextAccessor)
    {
        // Extract the current HTTP context, which might be null in non-HTTP contexts.
        var httpContext = httpContextAccessor?.HttpContext;

        // Extract the user's object ID from the claims principal (for Azure AD).
        var objectId = httpContext?.User.GetObjectId();

        // Get the trace identifier for request correlation, or generate one if not available.
        var httpTraceIdentifier = httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

        // Get the request path for context information.
        var httpRequestPath = httpContext?.Request.Path.Value;

        // Create a new immutable request context object.
        return new RequestContext(
            ObjectId: objectId,
            HttpTraceIdentifier: httpTraceIdentifier,
            HttpRequestPath: httpRequestPath);
    }

    /// <summary>
    /// Implementation of the request context interface that stores HTTP request metadata.
    /// </summary>
    /// <param name="ObjectId">The unique Azure AD object ID of the authenticated user.</param>
    /// <param name="HttpTraceIdentifier">The unique trace identifier for the current request.</param>
    /// <param name="HttpRequestPath">The request path that identified the requested resource.</param>
    private record RequestContext(
        string? ObjectId,
        string? HttpTraceIdentifier,
        string? HttpRequestPath) : IRequestContext;
}
