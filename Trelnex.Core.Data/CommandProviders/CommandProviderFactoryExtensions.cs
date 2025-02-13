using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Data;

public static class CommandProviderFactoryExtensions
{
    /// <summary>
    /// Adds the <see cref="ICommandProviderFactory"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="providerFactory"> The <see cref="ICommandProviderFactory"/> to add to the services.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCommandProviderFactory<T>(
        this IServiceCollection services,
        T providerFactory)
        where T : class, ICommandProviderFactory
    {
        // register the provider as an ICommandProviderFactory
        // for anything that needs the command provider status
        services.AddKeyedSingleton<ICommandProviderFactory>(
            typeof(T).Name,
            providerFactory);

        return services;
    }

    /// <summary>
    /// Gets the collection of <see cref="ICommandProviderFactory"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The collection of <see cref="ICommandProviderFactory"/> where the key is the name of the credential provider.</returns>
    internal static IReadOnlyDictionary<string, ICommandProviderFactory>? GetCommandProviderFactories(
        this IServiceCollection services)
    {
        // find any command provider factories
        return services
            .Where(sd =>
            {
                // must be a key service of type ICommandProviderFactory
                if (sd.ServiceType != typeof(ICommandProviderFactory)) return false;
                if (sd.ServiceKey is null or not string) return false;
                if (sd.KeyedImplementationInstance is null or not ICommandProviderFactory) return false;

                return true;
            })
            .ToImmutableSortedDictionary(
                keySelector: sd => (sd.ServiceKey as string)!,
                elementSelector: sd => (sd.KeyedImplementationInstance as ICommandProviderFactory)!);
    }
}
