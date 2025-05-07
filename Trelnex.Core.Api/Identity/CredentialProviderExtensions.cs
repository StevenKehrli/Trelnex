using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Identity;

/// <summary>
/// Provides extension methods for working with credential providers.
/// </summary>
/// <remarks>
/// These extensions simplify registration and retrieval of credential providers.
/// </remarks>
public static class CredentialProviderExtensions
{
    /// <summary>
    /// Registers a credential provider with the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The credential type that the provider manages.</typeparam>
    /// <param name="services">The service collection to register the provider with.</param>
    /// <param name="credentialProvider">The credential provider instance to register.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddCredentialProvider<T>(
        this IServiceCollection services,
        ICredentialProvider<T> credentialProvider)
    {
        var credentialProviderName = credentialProvider.Name;

        // Register the provider as a keyed singleton using its name as the key.
        // This allows retrieval by name for services that need tokens.
        services.AddKeyedSingleton<ICredentialProvider>(
            credentialProviderName,
            credentialProvider);

        return services;
    }

    /// <summary>
    /// Retrieves a credential provider by name from the service collection.
    /// </summary>
    /// <param name="services">The service collection to search.</param>
    /// <param name="credentialProviderName">The name of the credential provider to retrieve.</param>
    /// <returns>The specified credential provider instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no credential provider with the specified name is registered.
    /// </exception>
    public static ICredentialProvider GetCredentialProvider(
        this IServiceCollection services,
        string credentialProviderName)
    {
        // Find the credential provider with the matching name.
        var serviceDescriptor = services.FirstOrDefault(sd =>
        {
            if (sd.IsCredentialProvider() is false) return false;

            // Match by the service key (provider name).
            return string.Equals(sd.ServiceKey as string, credentialProviderName);
        });

        // Return the provider or throw if not found.
        return serviceDescriptor?.KeyedImplementationInstance as ICredentialProvider
            ?? throw new InvalidOperationException($"ICredentialProvider '{credentialProviderName}' is not registered.");
    }

    /// <summary>
    /// Retrieves a typed credential provider from the service collection.
    /// </summary>
    /// <typeparam name="T">The credential type that the provider manages.</typeparam>
    /// <param name="services">The service collection to search.</param>
    /// <returns>The typed credential provider instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no credential provider for the specified type is registered.
    /// </exception>
    public static ICredentialProvider<T> GetCredentialProvider<T>(
        this IServiceCollection services)
    {
        // Find the first credential provider that handles the specified type.
        var serviceDescriptor = services.FirstOrDefault(sd =>
        {
            if (sd.IsCredentialProvider() is false) return false;

            // Check if the provider is of the requested generic type.
            return sd.KeyedImplementationInstance is ICredentialProvider<T>;
        });

        // Return the provider or throw if not found.
        return serviceDescriptor?.KeyedImplementationInstance as ICredentialProvider<T>
            ?? throw new InvalidOperationException($"'ICredentialProvider<{typeof(T).Name}>' is not registered.");
    }

    /// <summary>
    /// Retrieves all registered credential providers from the service collection.
    /// </summary>
    /// <param name="services">The service collection to search.</param>
    /// <returns>A collection of credential provider instances ordered by name.</returns>
    internal static IEnumerable<ICredentialProvider> GetCredentialProviders(
        this IServiceCollection services)
    {
        // Find all credential providers in the service collection.
        return services
            .Where(sd => sd.IsCredentialProvider())
            .Select(sd => sd.KeyedImplementationInstance)
            .Cast<ICredentialProvider>()
            .OrderBy(cp => cp.Name);
    }

    /// <summary>
    /// Determines if a service descriptor represents a credential provider.
    /// </summary>
    /// <param name="serviceDescriptor">The service descriptor to check.</param>
    /// <returns><see langword="true"/> if the descriptor represents a credential provider; otherwise, <see langword="false"/>.</returns>
    private static bool IsCredentialProvider(
        this ServiceDescriptor serviceDescriptor)
    {
        // Must be a keyed service of ICredentialProvider type.
        if (serviceDescriptor.ServiceType != typeof(ICredentialProvider)) return false;

        // Must have a valid implementation instance.
        if (serviceDescriptor.KeyedImplementationInstance is null or not ICredentialProvider) return false;

        // Must have a string key for retrieval.
        if (serviceDescriptor.ServiceKey is null or not string) return false;

        return true;
    }
}
