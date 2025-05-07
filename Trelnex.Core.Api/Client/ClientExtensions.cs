using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Client;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Client;

/// <summary>
/// Provides extension methods for configuring HTTP clients with authentication in the application.
/// </summary>
/// <remarks>
/// These extensions simplify the registration of typed HTTP clients with appropriate configuration
/// and authentication settings, supporting both authenticated and unauthenticated clients.
/// </remarks>
public static class ClientExtensions
{
    /// <summary>
    /// Registers a typed HTTP client with the dependency injection container.
    /// </summary>
    /// <typeparam name="IClient">The client interface type to register.</typeparam>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="configuration">The application configuration containing client settings.</param>
    /// <param name="clientFactory">The factory responsible for creating client instances.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    ///   <item>Reads client configuration from the "Clients:{clientName}" section</item>
    ///   <item>Sets up authentication if configured, using the appropriate credential provider</item>
    ///   <item>Configures the HttpClient with the base address from configuration</item>
    ///   <item>Uses the provided factory to create the client implementation</item>
    /// </list>
    ///
    /// The client configuration should include a BaseAddress and optional Authentication section.
    /// </remarks>
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

        // Get the client configuration from the appropriate section
        var clientConfiguration = configuration
            .GetSection("Clients")
            .GetSection(clientName)
            .Get<ClientConfiguration>()
            ?? throw new ConfigurationErrorsException($"Configuration error for 'Clients:{clientName}'.");

        // Set up the access token provider if authentication is configured
        IAccessTokenProvider? accessTokenProvider = null;
        if (clientConfiguration.Authentication is not null)
        {
            var credentialProvider = services.GetCredentialProvider(clientConfiguration.Authentication.CredentialProviderName);

            // Get the access token provider for the specified scope
            accessTokenProvider = credentialProvider.GetAccessTokenProvider(clientConfiguration.Authentication.Scope);
        }

        // Register the typed HTTP client with the DI container
        services.AddHttpClient<IClient, IClient>(httpClient =>
        {
            httpClient.BaseAddress = clientConfiguration.BaseAddress;

            return clientFactory.Create(httpClient, accessTokenProvider);
        });

        return services;
    }

    /// <summary>
    /// Configuration model for HTTP clients.
    /// </summary>
    /// <param name="BaseAddress">The base URI for the client.</param>
    /// <param name="Authentication">Optional authentication configuration.</param>
    /// <remarks>
    /// This record represents the structure expected in the "Clients:{clientName}" configuration section.
    /// The BaseAddress is required, while Authentication is optional for unauthenticated clients.
    /// </remarks>
    private record ClientConfiguration(
        Uri BaseAddress,
        AuthenticationConfiguration? Authentication = null);

    /// <summary>
    /// Configuration model for HTTP client authentication.
    /// </summary>
    /// <param name="CredentialProviderName">The name of the registered credential provider to use.</param>
    /// <param name="Scope">The OAuth scope required for API access.</param>
    /// <remarks>
    /// When present, this configuration enables authenticated requests by obtaining
    /// access tokens from the specified credential provider with the given scope.
    /// </remarks>
    private record AuthenticationConfiguration(
        string CredentialProviderName,
        string Scope);
}
