using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Identity;

public static class CredentialProviderExtensions
{
    /// <summary>
    /// Gets the <see cref="ICredentialProvider"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to get the service from.</param>
    /// <param name="credentialProviderType">The type of the credential provider to get.</param>
    /// <returns>The <see cref="AzureCredentialProvider"/>.</returns>
    public static ICredentialProvider GetCredentialProvider(
        this IServiceCollection services,
        string credentialProviderType)
    {
        var serviceDescriptor = services.FirstOrDefault(x =>
        {
            if (x.IsCredentialProvider() is false) return false;

            // where the service key equals the credential provider type
            return string.Equals(x.ServiceKey as string, credentialProviderType);
        });

        return serviceDescriptor?.KeyedImplementationInstance as ICredentialProvider
            ?? throw new InvalidOperationException($"'{credentialProviderType}' is not registered.");
    }

    /// <summary>
    /// Adds the <see cref="ICredentialProvider"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="credentialProvider">The <see cref="ICredentialProvider"/> to add to the services.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    internal static IServiceCollection AddCredentialProvider<T>(
        this IServiceCollection services,
        T credentialProvider)
        where T : class, ICredentialProvider
    {
        // register the provider as an ICredentialProvider
        // for anything that needs an access token or credential status
        services.AddKeyedSingleton<ICredentialProvider>(
            typeof(T).Name,
            credentialProvider);

        return services;
    }

    /// <summary>
    /// Gets the <see cref="ICredentialProvider"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to get the service from.</param>
    /// <returns>The <see cref="AzureCredentialProvider"/>.</returns>
    internal static T GetCredentialProvider<T>(
        this IServiceCollection services)
        where T : class, ICredentialProvider
    {
        var credentialProviderType = typeof(T).Name;

        var credentialProvider = services.GetCredentialProvider(credentialProviderType);

        return credentialProvider as T
            ?? throw new InvalidOperationException($"'{credentialProviderType}' is not registered.");
    }

    /// <summary>
    /// Gets the collection of <see cref="ICredentialProvider"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to get the services from.</param>
    /// <returns>The collection of <see cref="ICredentialProvider"/> where the key is the name of the credential provider.</returns>
    internal static IReadOnlyDictionary<string, ICredentialProvider>? GetCredentialProviders(
        this IServiceCollection services)
    {
        // find any credential providers
        return services
            .Where(x => x.IsCredentialProvider())
            .ToImmutableSortedDictionary(
                keySelector: x => (x.ServiceKey as string)!,
                elementSelector: x => (x.KeyedImplementationInstance as ICredentialProvider)!);
    }

    private static bool IsCredentialProvider(
        this ServiceDescriptor serviceDescriptor)
    {
        // must be a keyed service of type ICredentialProvider
        if (serviceDescriptor.ServiceType != typeof(ICredentialProvider)) return false;
        if (serviceDescriptor.KeyedImplementationInstance is null or not ICredentialProvider) return false;
        if (serviceDescriptor.ServiceKey is null or not string) return false;

        return true;
    }
}
