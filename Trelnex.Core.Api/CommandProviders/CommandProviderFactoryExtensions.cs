using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Provides extension methods for registering and retrieving command provider factories.
/// </summary>
/// <remarks>
/// Simplifies registration and supports health monitoring of data store connections.
/// </remarks>
public static class CommandProviderFactoryExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers a command provider factory with the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The command provider factory type.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="providerFactory">The command provider factory instance to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddCommandProviderFactory<T>(
        this IServiceCollection services,
        T providerFactory)
        where T : class, ICommandProviderFactory
    {
        // Register the provider as a keyed singleton service.
        // The type name is used as the key for retrieval.
        services.AddKeyedSingleton<ICommandProviderFactory>(
            typeof(T).Name,
            providerFactory);

        return services;
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Retrieves all registered command provider factories from the service collection.
    /// </summary>
    /// <param name="services">The service collection to search.</param>
    /// <returns>
    /// A dictionary of command provider factories keyed by their registration names,
    /// or <see langword="null"/> if none are found.
    /// </returns>
    internal static IReadOnlyDictionary<string, ICommandProviderFactory> GetCommandProviderFactories(
        this IServiceCollection services)
    {
        // Find all registered command provider factories.
        return services
            .Where(serviceDescriptor =>
            {
                // Must be a keyed service of ICommandProviderFactory type.
                if (serviceDescriptor.ServiceType != typeof(ICommandProviderFactory))
                {
                    return false;
                }

                // The service key must not be null and must be a string.
                if (serviceDescriptor.ServiceKey is null or not string)
                {
                    return false;
                }

                // The keyed implementation instance must not be null and must be an ICommandProviderFactory.
                if (serviceDescriptor.KeyedImplementationInstance is null or not ICommandProviderFactory)
                {
                    return false;
                }

                return true;
            })
            .ToImmutableSortedDictionary(
                keySelector: serviceDescription => (serviceDescription.ServiceKey as string)!,
                elementSelector: serviceDescription => (serviceDescription.KeyedImplementationInstance as ICommandProviderFactory)!);
    }

    #endregion
}
