using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Client;

namespace Trelnex.Core.Api.Client;

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
    public static IServiceCollection AddClient<IClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        IClientFactory<IClient> clientFactory)
        where IClient : class
    {
        var clientName = clientFactory.Name;

        // get the client configuration
        var clientConfiguration = configuration
            .GetSection("Clients")
            .GetSection(clientName)
            .Get<ClientConfiguration>()
            ?? throw new ConfigurationErrorsException($"Configuration error for 'Clients:{clientName}'.");

        // get the access token provider
        var getAccessTokenProvider = () =>
        {
            if (clientConfiguration.Authentication is null) return null;

            // get the credential provider
            var credentialProvider = services.GetCredentialProvider(clientConfiguration.Authentication.CredentialProviderName);

            // get the access token provider for the client
            return credentialProvider.GetAccessTokenProvider(clientConfiguration.Authentication.Scope);
        };

        var accessTokenProvider = getAccessTokenProvider();

        // add the client to the services
        services.AddHttpClient<IClient, IClient>(httpClient =>
        {
            httpClient.BaseAddress = clientConfiguration.BaseAddress;

            return clientFactory.Create(httpClient, accessTokenProvider);
        });

        return services;
    }

    /// <summary>
    /// Represents the configuration properties for a client.
    /// </summary>
    /// <param name="BaseAddress">The base address <see cref="Uri"/> to build the request <see cref="Uri"/>.</param>
    /// <param name="Authentication">The authentication configuration.</param>
    private record ClientConfiguration(
        Uri BaseAddress,
        AuthenticationConfiguration? Authentication = null);

    /// <summary>
    /// Represents the authentication configuration properties for a client.
    /// </summary>
    /// <param name="CredentialProviderName">The name of the credential provider.</param>
    /// <param name="Scope">The scope of the access token.</param>    
    private record AuthenticationConfiguration(
        string CredentialProviderName,
        string Scope);
}
