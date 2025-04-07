using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Amazon.Observability;
using Trelnex.Core.Api.Identity;

namespace Trelnex.Core.Amazon.Identity;

public static class AmazonIdentityExtensions
{
    /// <summary>
    /// Add the <see cref="CredentialFactory"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAmazonIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        services.AddAWSInstrumentation();

        var options = configuration
            .GetSection("AmazonCredentials")
            .Get<AmazonCredentialOptions>()
            ?? throw new ConfigurationErrorsException("The AmazonCredentials configuration is not found.");

        // create the credential provider
        var credentialProvider = AmazonCredentialProvider.Create(
            bootstrapLogger,
            options).Result;

        // register the provider
        services.AddCredentialProvider(credentialProvider);

        return services;
    }
}
