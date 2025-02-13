using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Identity;

public static class CredentialProviderExtensions
{
    /// <summary>
    /// Adds the <see cref="ICredentialProvider"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="credentialProvider">The <see cref="ICredentialProvider"/> to add to the services.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCredentialProvider<T>(
        this IServiceCollection services,
        ICredentialProvider<T> credentialProvider)
    {
        var credentialProviderName = credentialProvider.Name;

        // register the provider as an ICredentialProvider
        // for anything that needs an access token or credential status
        services.AddKeyedSingleton<ICredentialProvider>(
            credentialProviderName,
            credentialProvider);

        return services;
    }

    /// <summary>
    /// Gets the <see cref="ICredentialProvider"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to get the service from.</param>
    /// <param name="credentialProviderName">The name of the credential provider to get.</param>
    /// <returns>The <see cref="ICredentialProvider"/>.</returns>
    public static ICredentialProvider GetCredentialProvider(
        this IServiceCollection services,
        string credentialProviderName)
    {
        var serviceDescriptor = services.FirstOrDefault(sd =>
        {
            if (sd.IsCredentialProvider() is false) return false;

            // where the service key equals the credential provider name
            return string.Equals(sd.ServiceKey as string, credentialProviderName);
        });

        return serviceDescriptor?.KeyedImplementationInstance as ICredentialProvider
            ?? throw new InvalidOperationException($"ICredentialProvider '{credentialProviderName}' is not registered.");
    }

    /// <summary>
    /// Gets the <see cref="ICredentialProvider{T}"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to get the service from.</param>
    /// <returns>The <see cref="ICredentialProvider{T}"/>.</returns>
    public static ICredentialProvider<T> GetCredentialProvider<T>(
        this IServiceCollection services)
    {
        var serviceDescriptor = services.FirstOrDefault(sd =>
        {
            if (sd.IsCredentialProvider() is false) return false;

            // check if the KeyedImplementationInstance is of type ICredentialProvider<T>
            return sd.KeyedImplementationInstance is ICredentialProvider<T>;
        });

        return serviceDescriptor?.KeyedImplementationInstance as ICredentialProvider<T>
            ?? throw new InvalidOperationException($"'ICredentialProvider<{typeof(T).Name}>' is not registered.");
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
            .Where(sd => sd.IsCredentialProvider())
            .ToImmutableSortedDictionary(
                keySelector: sd => (sd.ServiceKey as string)!,
                elementSelector: sd => (sd.KeyedImplementationInstance as ICredentialProvider)!);
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
