using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Client;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Client;

/// <summary>
/// Provides extension methods for configuring HTTP clients with authentication.
/// </summary>
/// <remarks>
/// These extensions simplify the registration of typed HTTP clients.
/// </remarks>
public static class ClientExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Registers a typed HTTP client with the dependency injection container.
    /// </summary>
    /// <typeparam name="IClient">The client interface type to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="configuration">The application configuration containing client settings.</param>
    /// <param name="clientFactory">The factory responsible for creating client instances.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the client configuration section is missing or invalid.
    /// </exception>
    public static IServiceCollection AddClient<IClient>(
        this IServiceCollection services,
        IConfiguration configuration,
        IClientFactory<IClient> clientFactory)
        where IClient : class
    {
        var clientName = clientFactory.Name;

        // Get the client configuration from the appropriate section.
        var clientConfiguration = configuration
            .GetSection("Clients")
            .GetSection(clientName)
            .Get<ClientConfiguration>()
            ?? throw new ConfigurationErrorsException($"Configuration error for 'Clients:{clientName}'.");

        // Set up the access token provider if authentication is configured.
        IAccessTokenProvider? accessTokenProvider = null;
        if (clientConfiguration.Authentication is not null)
        {
            // Retrieve the credential provider based on the configured name.
            var credentialProvider = services.GetCredentialProvider(clientConfiguration.Authentication.CredentialProviderName);

            // Get the access token provider for the specified scope.
            accessTokenProvider = credentialProvider.GetAccessTokenProvider(clientConfiguration.Authentication.Scope);
        }

        // Register the typed HTTP client with the DI container.
        services.AddHttpClient<IClient, IClient>(httpClient =>
        {
            // Set the base address for the HTTP client.
            httpClient.BaseAddress = clientConfiguration.BaseAddress;

            // Create the client instance using the provided factory.
            return clientFactory.Create(httpClient, accessTokenProvider);
        });

        return services;
    }

    #endregion

    #region Private Types

    /// <summary>
    /// Configuration model for HTTP clients.
    /// </summary>
    /// <param name="BaseAddress">The base URI for the client.</param>
    /// <param name="Authentication">Optional authentication configuration.</param>
    private record ClientConfiguration(
        Uri BaseAddress,
        AuthenticationConfiguration? Authentication = null);

    /// <summary>
    /// Configuration model for HTTP client authentication.
    /// </summary>
    /// <param name="CredentialProviderName">The name of the registered credential provider to use.</param>
    /// <param name="Scope">The OAuth scope required for API access.</param>
    private record AuthenticationConfiguration(
        string CredentialProviderName,
        string Scope);

    #endregion
}
