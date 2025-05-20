using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Provides extension methods for configuring authentication and authorization services.
/// </summary>
/// <remarks>
/// Configures the application's authentication and authorization pipeline.
/// </remarks>
public static class AuthenticationExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds authentication and authorization services to the application's service collection.
    /// </summary>
    /// <param name="services">The service collection to add authentication services to.</param>
    /// <param name="configuration">The application configuration containing authentication settings.</param>
    /// <returns>A <see cref="IPoliciesBuilder"/> to further configure authentication permissions.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication services were already configured.
    /// </exception>
    public static IPermissionsBuilder AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ThrowIfSecurityProviderAdded();

        services.AddHttpContextAccessor();
        services.AddInMemoryTokenCaches();

        services.AddSingleton<IAuthorizationHandler, PermissionRequirementAuthorizationHandler>();

        // Inject our security provider.
        var securityProvider = new SecurityProvider();
        services.AddSingleton<ISecurityProvider>(securityProvider);

        // Add the permissions to the security provider and return the builder for further configuration.
        return new PermissionsBuilder(services, configuration, securityProvider);
    }

    /// <summary>
    /// Configures the application with no authentication, suitable for development or testing.
    /// </summary>
    /// <param name="services">The service collection to configure with no authentication.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication services were already configured.
    /// </exception>
    public static void NoAuthentication(
        this IServiceCollection services)
    {
        services.ThrowIfSecurityProviderAdded();

        services.AddHttpContextAccessor();

        services.AddAuthentication();
        services.AddAuthorization();

        // Inject an empty security provider for scenarios where authentication is not required.
        var securityProvider = new SecurityProvider();
        services.AddSingleton<ISecurityProvider>(securityProvider);
    }

    /// <summary>
    /// Verifies that authentication has been configured for the application.
    /// </summary>
    /// <param name="services">The service collection to check for authentication configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication has not been configured.
    /// </exception>
    public static void ThrowIfAuthenticationNotAdded(
        this IServiceCollection services)
    {
        // Check if the security provider was added.
        var added = services.Any(sd => sd.ServiceType == typeof(ISecurityProvider));

        if (added is false)
        {
            throw new InvalidOperationException("Authentication has not been configured.");
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Verifies that authentication has not been configured multiple times.
    /// </summary>
    /// <param name="services">The service collection to check for existing authentication configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication has already been configured.
    /// </exception>
    private static void ThrowIfSecurityProviderAdded(
        this IServiceCollection services)
    {
        // Check if the security provider was added.
        var added = services.Any(sd => sd.ServiceType == typeof(ISecurityProvider));

        if (added is true)
        {
            throw new InvalidOperationException($"{nameof(AddAuthentication)} or {nameof(NoAuthentication)} has already been configured.");
        }
    }

    #endregion
}
