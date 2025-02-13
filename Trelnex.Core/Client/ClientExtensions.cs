using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Client;

/// <summary>
/// Extension methods to add the <see cref="IClient"/> to the <see cref="IServiceCollection"/>.
/// </summary>
public static class ClientExtensions
{
    /// <summary>
    /// Add the <see cref="IClient"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddClient<IClient, TClient>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TClient : BaseClient, IClient
        where IClient : class
    {
        var clientName = typeof(TClient).Name;

        // get the client configuration
        var clientConfiguration = configuration
            .GetSection("Clients")
            .GetSection(clientName)
            .Get<ClientConfiguration>()
            .ValidateOrThrow(clientName);

        // get the credential provider
        var credentialProvider = services.GetCredentialProvider(clientConfiguration.CredentialProviderName);

        // get the access token provider for the client
        var accessTokenProvider = credentialProvider.GetAccessTokenProvider<TClient>(
            scope: clientConfiguration.Scope);

        // add the access token provider to the services
        services.AddSingleton(accessTokenProvider);

        // add the client to the services
        services.AddHttpClient<IClient, TClient>(httpClient => httpClient.BaseAddress = clientConfiguration.BaseAddress);

        return services;
    }

    /// <summary>
    /// Validates the client configuration; throw if not valid.
    /// </summary>
    /// <param name="clientConfiguration">The <see cref="ClientConfiguration"/>.</param>
    /// <param name="clientName">The name of the client.</param>
    /// <returns>The valid <see cref="ClientConfiguration"/>.</returns>
    /// <exception cref="ConfigurationErrorsException">The exception that is thrown when a configuration error has occurred.</exception>
    private static ClientConfiguration ValidateOrThrow(
        this ClientConfiguration? clientConfiguration,
        string clientName)
    {
        return Validate(clientConfiguration)
            ? clientConfiguration!
            : throw new ConfigurationErrorsException($"Configuration error for 'Clients:{clientName}'.");
    }

    /// <summary>
    /// Validates the client configuration.
    /// </summary>
    /// <param name="clientConfiguration">The <see cref="ClientConfiguration"/>.</param>
    /// <returns>true if the <see cref="ClientConfiguration"/> is valid; otherwise, false.</returns>
    private static bool Validate(
        ClientConfiguration? clientConfiguration)
    {
        if (clientConfiguration?.BaseAddress is null) return false;
        if (string.IsNullOrWhiteSpace(clientConfiguration?.CredentialProviderName)) return false;
        if (string.IsNullOrWhiteSpace(clientConfiguration?.Scope)) return false;

        return true;
    }
}
