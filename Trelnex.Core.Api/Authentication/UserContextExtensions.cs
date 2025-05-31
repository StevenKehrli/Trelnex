using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Provides extension methods for configuring and accessing <see cref="IUserContext"/> data.
/// </summary>
public static class UserContextExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers the <see cref="IUserContext"/> service with dependency injection.
    /// </summary>
    /// <param name="services">The service collection to add the context to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddUserContext(
        this IServiceCollection services)
    {
        services.AddScoped<IUserContext>(serviceProvider =>
        {
            // Access the HTTP context through the accessor service.
            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // Extract the current HTTP context, which might be null in non-HTTP contexts.
            var httpContext = httpContextAccessor?.HttpContext;

            var userContext = httpContext?.GetUserContext();

            if (userContext is not null) return userContext;

            return new UserContext(
                user: httpContext?.User,
                authorizedPolicies: []);
        });

        return services;
    }

    /// <summary>
    /// Retrieves the <see cref="UserContext"/> from the <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> to retrieve the <see cref="UserContext"/> from.</param>
    /// <returns>The <see cref="UserContext"/> if it exists in the <see cref="HttpContext"/>; otherwise, <c>null</c>.</returns>
    internal static UserContext? GetUserContext(
        this HttpContext httpContext)
    {
        // Retrieve the user context from the HTTP context's Items collection.
        httpContext.Items.TryGetValue(typeof(UserContext), out var value);
        return value as UserContext;
    }

    /// <summary>
    /// Sets the <see cref="UserContext"/> in the <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> in which to set the <see cref="UserContext"/>.</param>
    /// <param name="userContext">The <see cref="UserContext"/> to set.</param>
    internal static void SetUserContext(
        this HttpContext httpContext,
        UserContext userContext)
    {
        // Store the user context in the HTTP context's Items collection.
        httpContext.Items[typeof(UserContext)] = userContext;
    }

    #endregion
}
