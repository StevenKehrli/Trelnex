using Amazon.Runtime;
using Trelnex.Auth.Amazon.Services.CallerIdentity;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Identity;

namespace Trelnex.Auth.Amazon.Services;

internal static class ServicesExtensions
{
    /// <summary>
    /// Add the necessary services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the bootstrap logs.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        // get the aws credentials provider
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();



        // create and inject the validators
        var scopeValidator = new ScopeValidator();
        services.AddSingleton<IScopeValidator>(scopeValidator);

        var resourceNameValidator = new ResourceNameValidator();
        services.AddSingleton<IResourceNameValidator>(resourceNameValidator);

        var scopeNameValidator = new ScopeNameValidator();
        services.AddSingleton<IScopeNameValidator>(scopeNameValidator);

        var roleNameValidator = new RoleNameValidator();
        services.AddSingleton<IRoleNameValidator>(roleNameValidator);



        // create the jwt provider registry
        var jwtProviderRegistry = JwtProviderRegistry.Create(
            configuration,
            bootstrapLogger,
            credentialProvider);

        // inject the jwt provider registry
        services.AddSingleton(jwtProviderRegistry);



        // create the rbac repository
        var rbacRepository = RBACRepository.Create(
            configuration,
            scopeNameValidator,
            credentialProvider);

        // inject the rbac repository
        services.AddSingleton<IRBACRepository>(rbacRepository);



        // create the caller identity provider
        var callerIdentityProvider = CallerIdentityProvider.Create(
            credentialProvider);

        // inject the caller identity provider
        services.AddSingleton<ICallerIdentityProvider>(callerIdentityProvider);



        return services;
    }
}
