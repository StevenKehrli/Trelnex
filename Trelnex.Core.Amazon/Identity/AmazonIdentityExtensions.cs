using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Amazon.Observability;
using Trelnex.Core.Api.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Extension methods for configuring Amazon Identity.
/// </summary>
/// <remarks>
/// Registers Amazon Identity services with the dependency injection container.
/// </remarks>
public static class AmazonIdentityExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds Amazon Identity services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection to add the services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="bootstrapLogger">The logger for setup.</param>
    /// <returns>The same service collection.</returns>
    /// <exception cref="ConfigurationErrorsException">Thrown when the "Amazon.Credentials" configuration section is not found.</exception>
    /// <remarks>
    /// Configures and registers an <see cref="AmazonCredentialProvider"/> for authentication with AWS.
    /// Adds AWS instrumentation for observability.
    /// </remarks>
    public static async Task<IServiceCollection> AddAmazonIdentityAsync(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        // Add AWS instrumentation for observability
        services.AddAWSInstrumentation();

        // Extract Amazon credential options from the configuration
        var options = configuration
            .GetSection("Amazon.Credentials")
            .Get<AmazonCredentialOptions>()
            ?? throw new ConfigurationErrorsException("The Amazon.Credentials configuration is not valid.");

        // Create the credential provider using the extracted options
        var credentialProvider = await AmazonCredentialProvider
            .CreateAsync(options, bootstrapLogger);

        // Register the credential provider for dependency injection
        services.AddCredentialProvider(credentialProvider);

        // Return the service collection to allow for method chaining
        return services;
    }

    #endregion
}
