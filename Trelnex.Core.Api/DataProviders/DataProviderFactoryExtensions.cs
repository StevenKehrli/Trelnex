using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.DataProviders;

/// <summary>
/// Provides extension methods for registering and retrieving data provider factories.
/// </summary>
/// <remarks>
/// Simplifies registration and supports health monitoring of data store connections.
/// </remarks>
public static class DataProviderFactoryExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers a data provider factory with the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The data provider factory type.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="providerFactory">The data provider factory instance to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDataProviderFactory<T>(
        this IServiceCollection services,
        T providerFactory)
        where T : IDataProviderFactory
    {
        // Register the provider as a keyed singleton service.
        // The type name is used as the key for retrieval.
        services.AddKeyedSingleton<IDataProviderFactory>(
            typeof(T).Name,
            providerFactory);

        return services;
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Retrieves all registered data provider factories from the service collection.
    /// </summary>
    /// <param name="services">The service collection to search.</param>
    /// <returns>
    /// A dictionary of data provider factories keyed by their registration names,
    /// or <see langword="null"/> if none are found.
    /// </returns>
    internal static IReadOnlyDictionary<string, IDataProviderFactory> GetDataProviderFactories(
        this IServiceCollection services)
    {
        // Find all registered data provider factories.
        return services
            .Where(serviceDescriptor =>
            {
                // Must be a keyed service of IDataProviderFactory type.
                if (serviceDescriptor.ServiceType != typeof(IDataProviderFactory))
                {
                    return false;
                }

                // The service key must not be null and must be a string.
                if (serviceDescriptor.ServiceKey is null or not string)
                {
                    return false;
                }

                // The keyed implementation instance must not be null and must be an IDataProviderFactory.
                if (serviceDescriptor.KeyedImplementationInstance is null or not IDataProviderFactory)
                {
                    return false;
                }

                return true;
            })
            .ToImmutableSortedDictionary(
                keySelector: serviceDescription => (serviceDescription.ServiceKey as string)!,
                elementSelector: serviceDescription => (serviceDescription.KeyedImplementationInstance as IDataProviderFactory)!);
    }

    #endregion
}
