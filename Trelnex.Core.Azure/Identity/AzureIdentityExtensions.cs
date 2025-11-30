using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Api.Identity;

namespace Trelnex.Core.Azure.Identity;

/// <summary>
/// Extension methods for configuring Azure Identity services.
/// </summary>
/// <remarks>
/// Enables registration of Azure Identity services with the dependency injection container.
/// </remarks>
public static class AzureIdentityExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds Azure Identity services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration containing Azure credential settings.</param>
    /// <param name="bootstrapLogger">The logger for setup and initialization information.</param>
    /// <returns>The same service collection to enable method chaining.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when the "Azure.Credentials" configuration section is not valid.</exception>
    /// <remarks>
    /// <para>
    /// Configures and registers an <see cref="AzureCredentialProvider"/> for authentication with Azure services.
    /// </para>
    /// <para>
    /// Creates a <see cref="ChainedTokenCredential"/> from the configured sources, wraps it in a
    /// <see cref="ManagedCredential"/> for token caching and automatic refresh.
    /// </para>
    /// <para>
    /// Expects an "Azure.Credentials" section in the configuration, containing <see cref="AzureCredentialOptions"/>.
    /// </para>
    ///
    /// Example configuration:
    /// <code>
    /// {
    ///   "Azure.Credentials": {
    ///     "Sources": [ "WorkloadIdentity", "AzureCli" ]
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public static async Task<IServiceCollection> AddAzureIdentityAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        // Extract Azure credential options from the configuration
        var credentialOptions = configuration
            .GetSection("Azure.Credentials")
            .Get<AzureCredentialOptions>()
            ?? throw new ConfigurationErrorsException("The Azure.Credentials configuration is not valid");

        // Create the credential provider using the extracted options
        var credentialProvider = await AzureCredentialProvider
            .CreateAsync(credentialOptions, bootstrapLogger);

        // Register the credential provider for dependency injection
        services.AddCredentialProvider(credentialProvider);

        // Return the service collection to allow for method chaining
        return services;
    }

    #endregion
}
