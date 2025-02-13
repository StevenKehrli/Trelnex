using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Extension methods to add Authentication and Authorization to the <see cref="IServiceCollection"/>.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Add Authentication and Authorization to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IPermissionsBuilder AddAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ThrowIfSecurityProviderAdded();

        services.AddInMemoryTokenCaches();

        // inject our security provider
        var securityProvider = new SecurityProvider();
        services.AddSingleton<ISecurityProvider>(securityProvider);

        // add the permissions to the security provider
        return new PermissionsBuilder(services, configuration, securityProvider);
    }

    /// <summary>
    /// Add Authentication and Authorization to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
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

    public static void ThrowIfAuthenticationNotAdded(
        this IServiceCollection services)
    {
        // check if authentication was added
        var added = services.Any(sd => sd.ServiceType == typeof(IAuthenticationService));

        if (added is false)
        {
            throw new InvalidOperationException("Authentication has not been configured.");
        }
    }

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
