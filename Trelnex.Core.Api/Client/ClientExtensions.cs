using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Client;

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
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the client configuration section is missing or invalid.
    /// </exception>
    public static IServiceCollection AddClient<IClient, TClient>(
        this IServiceCollection services,
        IConfiguration configuration)
        where IClient : class
        where TClient: BaseClient, IClient
    {
        // Extract the client name from the TClient type name to locate configuration
        var clientName = typeof(TClient).Name;

        // Get the client configuration from the appropriate section.
        var clientConfiguration = configuration
            .GetSection("Clients")
            .GetSection(clientName)
            .Get<ClientConfiguration>()
            ?? throw new ConfigurationErrorsException($"Configuration error for 'Clients:{clientName}'.");

        // Register the typed HTTP client with dependency injection
        var httpClientBuilder = services.AddHttpClient<IClient, TClient>();

        // Configure the HTTP client
        httpClientBuilder.ConfigureHttpClient(httpClient =>
        {
            // Set the base address for the HTTP client.
            httpClient.BaseAddress = clientConfiguration.BaseAddress;
        });

        // Configure authentication if specified in the client configuration
        if (clientConfiguration.Authentication is not null)
        {
            var authenticationConfiguration = clientConfiguration.Authentication;

            // Add custom authentication handler to the HTTP client pipeline
            httpClientBuilder.AddHttpMessageHandler(serviceProvider =>
            {
                // Resolve the credential provider by name from the service container
                var credentialProvider = services.GetCredentialProvider(authenticationConfiguration.CredentialProviderName);

                // Obtain an access token provider configured for the required OAuth scope
                var accessTokenProvider = credentialProvider.GetAccessTokenProvider(authenticationConfiguration.Scope);

                // Create and return the authentication handler
                return new AuthenticationHandler(accessTokenProvider);
            });
        }

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
