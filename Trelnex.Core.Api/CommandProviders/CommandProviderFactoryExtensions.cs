using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.CommandProviders;

/// <summary>
/// Provides extension methods for registering and retrieving command provider factories.
/// </summary>
/// <remarks>
/// Command provider factories create data access components that implement the command pattern
/// for different data stores. These extensions simplify registration with dependency injection
/// and support health monitoring of data store connections.
/// </remarks>
public static class CommandProviderFactoryExtensions
{
    /// <summary>
    /// Registers a command provider factory with the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The command provider factory type.</typeparam>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="providerFactory">The command provider factory instance to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers the provider factory as a keyed singleton service using the type name as the key.
    /// This approach allows multiple provider factories to be registered and retrieved by name.
    ///
    /// Command provider factories are responsible for creating command providers that implement
    /// the repository pattern for specific data stores (e.g., SQL, Cosmos DB, DynamoDB).
    /// </remarks>
    public static IServiceCollection AddCommandProviderFactory<T>(
        this IServiceCollection services,
        T providerFactory)
        where T : class, ICommandProviderFactory
    {
        // Register the provider as a keyed singleton service
        // The type name is used as the key for retrieval
        services.AddKeyedSingleton<ICommandProviderFactory>(
            typeof(T).Name,
            providerFactory);

        return services;
    }

    /// <summary>
    /// Retrieves all registered command provider factories from the service collection.
    /// </summary>
    /// <param name="services">The service collection to search.</param>
    /// <returns>
    /// A dictionary of command provider factories keyed by their registration names,
    /// or <see langword="null"/> if none are found.
    /// </returns>
    /// <remarks>
    /// This method is used internally to locate all registered command provider factories,
    /// typically for health check registration. It filters the service descriptors
    /// to find keyed instances of <see cref="ICommandProviderFactory"/>.
    /// </remarks>
    internal static IReadOnlyDictionary<string, ICommandProviderFactory> GetCommandProviderFactories(
        this IServiceCollection services)
    {
        // Find all registered command provider factories
        return services
            .Where(sd =>
            {
                // Must be a keyed service of ICommandProviderFactory type
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
