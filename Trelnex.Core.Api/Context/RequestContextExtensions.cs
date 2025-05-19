using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Context;

/// <summary>
/// Provides extension methods for configuring and accessing request context data.
/// </summary>
/// <remarks>
/// Makes HTTP request information accessible throughout the application.
/// </remarks>
public static class RequestContextExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers the request context service with dependency injection.
    /// </summary>
    /// <param name="services">The service collection to add the context to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRequestContext(
        this IServiceCollection services)
    {
        services.AddScoped(serviceProvider =>
        {
            // Access the HTTP context through the accessor service.
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // Create a request context from the current HTTP context.
            return GetRequestContext(httpContextAccessor);
        });

        return services;
    }

    #endregion

    #region Private Static Methods

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

        // Create a new immutable request context object.
        return new RequestContext(
            ObjectId: objectId);
    }

    #endregion

    #region Private Types

    /// <summary>
    /// Implementation of the request context interface that stores HTTP request metadata.
    /// </summary>
    /// <param name="ObjectId">The unique Azure AD object ID of the authenticated user.</param>
    private record RequestContext(
        string? ObjectId)
        : IRequestContext;

    #endregion
}
