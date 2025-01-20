using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Azure.Identity;

public static class AzureIdentityExtensions
{
    /// <summary>
    /// Add the <see cref="CredentialFactory"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAzureIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        var options =
            configuration.GetSection("AzureCredentials").Get<AzureCredentialOptions>()
            ?? throw new ConfigurationErrorsException("The AzureCredentials configuration is not found.");

        // create the credential provider
        var credentialProvider = AzureCredentialProvider.Create(
            bootstrapLogger,
            options);
        
        // register the provider
        services.AddCredentialProvider(credentialProvider);

        return services;
    }

    /// <summary>
    /// Gets the <see cref="AzureCredentialProvider"/> from the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="AzureCredentialProvider"/>.</returns>
    internal static AzureCredentialProvider GetAzureCredentialProvider(
        this IServiceCollection services)
    {
        return services.GetCredentialProvider<AzureCredentialProvider>();
    }
}
