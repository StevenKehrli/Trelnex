using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.Context;

/// <summary>
/// Extension methods to add the <see cref="IRequestContext"/> to the <see cref="IServiceCollection"/>.
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>
    /// Add the <see cref="IRequestContext"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRequestContext(
        this IServiceCollection services)
    {
        services
            .AddScoped(provider =>
            {
                // get the http context accessor
                var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();

                return GetRequestContext(httpContextAccessor);
            });

        return services;
    }

    /// <summary>
    /// Get the <see cref="IRequestContext"/> to represent the request context.
    /// </summary>
    /// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> to provide access to the current HttpContext.</param>
    /// <returns></returns>
    private static IRequestContext GetRequestContext(
        IHttpContextAccessor? httpContextAccessor)
    {
        var httpContext = httpContextAccessor?.HttpContext;

        var objectId = httpContext?.User.GetObjectId();

        var httpTraceIdentifier = httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();

        var httpRequestPath = httpContext?.Request.Path.Value;

        return new RequestContext(
            ObjectId: objectId,
            HttpTraceIdentifier: httpTraceIdentifier,
            HttpRequestPath: httpRequestPath);
    }

    /// <summary>
    /// Represents the request context
    /// </summary>
    /// <param name="ObjectId">Gets the unique object ID associated with the ClaimsPrincipal for this request.</param>
    /// <param name="HttpTraceIdentifier">Gets the unique identifier to represent this request in trace logs.</param>
    /// <param name="HttpRequestPath">Gets the portion of the request path that identifies the requested resource.</param>
    private record RequestContext(
        string? ObjectId,
        string? HttpTraceIdentifier,
        string? HttpRequestPath) : IRequestContext;
}
