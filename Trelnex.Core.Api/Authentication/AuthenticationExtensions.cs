using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Provides extension methods for configuring authentication and authorization services in an ASP.NET Core application.
/// </summary>
/// <remarks>
/// These extension methods configure the application's authentication and authorization pipeline
/// with standardized security providers and token validation. The methods follow a fluent builder
/// pattern to allow for clean, readable configuration code.
///
/// Security can be configured either with active authentication (JWT Bearer or Microsoft Identity)
/// or without authentication for development or testing scenarios.
/// </remarks>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authentication and authorization services to the application's service collection.
    /// </summary>
    /// <param name="services">The service collection to add authentication services to.</param>
    /// <param name="configuration">The application configuration containing authentication settings.</param>
    /// <returns>A <see cref="IPermissionsBuilder"/> to further configure authentication permissions.</returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    ///   <item>Configures HTTP context access for authentication</item>
    ///   <item>Adds in-memory token caching for better performance</item>
    ///   <item>Registers the security provider for policy enforcement</item>
    ///   <item>Returns a builder to define specific permission policies</item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication services were already configured for this application.
    /// </exception>
    public static IPermissionsBuilder AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ThrowIfSecurityProviderAdded();

        services.AddHttpContextAccessor();
        services.AddInMemoryTokenCaches();

        // inject our security provider
        var securityProvider = new SecurityProvider();
        services.AddSingleton<ISecurityProvider>(securityProvider);

        // add the permissions to the security provider
        return new PermissionsBuilder(services, configuration, securityProvider);
    }

    /// <summary>
    /// Configures the application with no authentication, suitable for development or testing scenarios.
    /// </summary>
    /// <param name="services">The service collection to configure with no authentication.</param>
    /// <remarks>
    /// This method configures minimal authentication components without actual token validation:
    /// <list type="bullet">
    ///   <item>Registers HTTP context accessor for consistency with authenticated scenarios</item>
    ///   <item>Adds empty authentication and authorization services</item>
    ///   <item>Registers an empty security provider that won't enforce authentication</item>
    /// </list>
    ///
    /// This approach maintains the same API surface for authenticated and non-authenticated scenarios
    /// while allowing applications to bypass authentication for development or testing.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication services were already configured for this application.
    /// </exception>
    public static void NoAuthentication(
        this IServiceCollection services)
    {
        services.ThrowIfSecurityProviderAdded();

        services.AddHttpContextAccessor();

        services.AddAuthentication();
        services.AddAuthorization();

        // inject an empty security provider
        var securityProvider = new SecurityProvider();
        services.AddSingleton<ISecurityProvider>(securityProvider);
    }

    /// <summary>
    /// Verifies that authentication has been configured for the application.
    /// </summary>
    /// <param name="services">The service collection to check for authentication configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication has not been configured for this application.
    /// </exception>
    /// <remarks>
    /// This method is used internally to ensure that authentication is properly configured
    /// before attempting to use authentication-dependent features.
    /// </remarks>
    public static void ThrowIfAuthenticationNotAdded(
        this IServiceCollection services)
    {
        // check if security provider was added
        var added = services.Any(sd => sd.ServiceType == typeof(ISecurityProvider));

        if (added is false)
        {
            throw new InvalidOperationException("Authentication has not been configured.");
        }
    }

    /// <summary>
    /// Verifies that authentication has not been configured multiple times.
    /// </summary>
    /// <param name="services">The service collection to check for existing authentication configuration.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if authentication has already been configured for this application.
    /// </exception>
    /// <remarks>
    /// This internal method prevents double-registration of authentication services,
    /// which could lead to unpredictable behavior or security vulnerabilities.
    /// </remarks>
    private static void ThrowIfSecurityProviderAdded(
        this IServiceCollection services)
    {
        // check if security provider was added
        var added = services.Any(sd => sd.ServiceType == typeof(ISecurityProvider));

        if (added is true)
        {
            throw new InvalidOperationException($"{nameof(AddAuthentication)} or {nameof(NoAuthentication)} has already been configured.");
        }
    }
}
