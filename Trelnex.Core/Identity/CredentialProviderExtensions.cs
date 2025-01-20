using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Identity;

public static class CredentialProviderExtensions
{
    /// <summary>
    /// Gets the <see cref="ICredentialProvider"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to get the service from.</param>
    /// <returns>The <see cref="AzureCredentialProvider"/>.</returns>
    public static ICredentialProvider GetCredentialProvider(
        this IServiceCollection services,
        string credentialProviderName)
    {
        // get the collection of all 
        var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType.Name == credentialProviderName);

        if (serviceDescriptor?.ImplementationInstance is not ICredentialProvider credentialProvider)
        {
            throw new InvalidOperationException($"'{credentialProviderName}' is not registered.");
        }

        return credentialProvider;
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
        // register the provider as itself
        // for anything that needs an Azure TokenCredential
        services.AddSingleton<T>(credentialProvider);

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
        where T : ICredentialProvider
    {
        var serviceDescriptor = services.FirstOrDefault(x => x.ServiceType == typeof(T));

        if (serviceDescriptor?.ImplementationInstance is not T credentialProvider)
        {
            throw new InvalidOperationException($"'{typeof(T)}' is not registered.");
        }

        return credentialProvider;
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
            .Where(x =>
            {
                // must be a key service of type ICredentialProvider
                if (x.ServiceType != typeof(ICredentialProvider)) return false;
                if (x.ServiceKey is null or not string) return false;
                if (x.KeyedImplementationInstance is null or not ICredentialProvider) return false;

                return true;
            })
            .ToImmutableSortedDictionary(
                keySelector: x => (x.ServiceKey as string)!,
                elementSelector: x => (x.KeyedImplementationInstance as ICredentialProvider)!);
    }
}
