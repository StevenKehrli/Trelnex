using Amazon.Runtime;
using Trelnex.Auth.Amazon.Services.CallerIdentity;
using Trelnex.Auth.Amazon.Services.JWT;
using Trelnex.Auth.Amazon.Services.RBAC;
using Trelnex.Auth.Amazon.Services.Validators;
using Trelnex.Core.Api.Identity;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services;

/// <summary>
/// Provides extension methods for registering authentication and authorization services with the dependency injection container.
/// </summary>
/// <remarks>
/// This class is responsible for configuring all the required services for the Trelnex.Auth.Amazon module,
/// including validators, JWT providers, RBAC repositories, and caller identity providers. These services
/// work together to provide a comprehensive authentication and authorization system leveraging AWS services.
///
/// The service registration follows a pattern where:
/// 1. Services are created with their dependencies
/// 2. Services are registered with the DI container using appropriate lifetimes (typically singleton)
/// 3. Services are registered with their interface types to promote loose coupling
///
/// This centralized registration approach simplifies application startup and ensures proper dependency resolution.
/// </remarks>
internal static class ServicesExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Adds all necessary authentication and authorization services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the bootstrap logs during service initialization.</param>
    /// <returns>The <see cref="IServiceCollection"/> with all required services registered.</returns>
    /// <remarks>
    /// This method orchestrates the registration of four primary service categories:
    /// 1. Validators - For validating names and properties in the RBAC system
    /// 2. JWT Provider Registry - For managing JWT token issuance and validation
    /// 3. RBAC Repository - For managing role-based access control data
    /// 4. Caller Identity Provider - For resolving AWS caller identities
    ///
    /// Each service is created with its required dependencies and registered with the DI container.
    /// All services are registered as singletons to ensure consistent state across the application.
    /// </remarks>
    public static IServiceCollection AddServices(
        this IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        // Get the AWS credentials provider from the service collection.
        var credentialProvider = services.GetCredentialProvider<AWSCredentials>();

        // Register validators.
        RegisterValidators(services);

        // Register JWT provider registry.
        RegisterJwtProviderRegistry(services, configuration, bootstrapLogger, credentialProvider);

        // Register RBAC repository.
        RegisterRbacRepository(services, configuration, credentialProvider);

        // Register caller identity provider.
        RegisterCallerIdentityProvider(services, credentialProvider);

        return services;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Registers all validators required for the RBAC system.
    /// </summary>
    /// <param name="services">The service collection to register validators with.</param>
    private static void RegisterValidators(IServiceCollection services)
    {
        // Create and inject the scope validator.
        var scopeValidator = new ScopeValidator();
        services.AddSingleton<IScopeValidator>(scopeValidator);

        // Create and inject the resource name validator.
        var resourceNameValidator = new ResourceNameValidator();
        services.AddSingleton<IResourceNameValidator>(resourceNameValidator);

        // Create and inject the scope name validator.
        var scopeNameValidator = new ScopeNameValidator();
        services.AddSingleton<IScopeNameValidator>(scopeNameValidator);

        // Create and inject the role name validator.
        var roleNameValidator = new RoleNameValidator();
        services.AddSingleton<IRoleNameValidator>(roleNameValidator);
    }

    /// <summary>
    /// Registers the JWT provider registry for token management.
    /// </summary>
    /// <param name="services">The service collection to register the JWT provider with.</param>
    /// <param name="configuration">Application configuration containing JWT settings.</param>
    /// <param name="bootstrapLogger">Logger for recording initialization events.</param>
    /// <param name="credentialProvider">AWS credential provider for KMS operations.</param>
    private static void RegisterJwtProviderRegistry(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Create the JWT provider registry with required dependencies.
        var jwtProviderRegistry = JwtProviderRegistry.Create(
            configuration,
            bootstrapLogger,
            credentialProvider);

        // Inject the JWT provider registry into the DI container.
        services.AddSingleton(jwtProviderRegistry);
    }

    /// <summary>
    /// Registers the RBAC repository for managing authorization data.
    /// </summary>
    /// <param name="services">The service collection to register the RBAC repository with.</param>
    /// <param name="configuration">Application configuration containing RBAC storage settings.</param>
    /// <param name="credentialProvider">AWS credential provider for data access.</param>
    private static void RegisterRbacRepository(
        IServiceCollection services,
        IConfiguration configuration,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Get the scope name validator from services (already registered).
        var scopeNameValidator = services.BuildServiceProvider().GetRequiredService<IScopeNameValidator>();

        // Create the RBAC repository with required dependencies.
        var rbacRepository = RBACRepository.Create(
            configuration,
            scopeNameValidator,
            credentialProvider);

        // Inject the RBAC repository into the DI container.
        services.AddSingleton<IRBACRepository>(rbacRepository);
    }

    /// <summary>
    /// Registers the caller identity provider for AWS identity resolution.
    /// </summary>
    /// <param name="services">The service collection to register the caller identity provider with.</param>
    /// <param name="credentialProvider">AWS credential provider for STS operations.</param>
    private static void RegisterCallerIdentityProvider(
        IServiceCollection services,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Create the caller identity provider with required dependencies.
        var callerIdentityProvider = CallerIdentityProvider.Create(
            credentialProvider);

        // Inject the caller identity provider into the DI container.
        services.AddSingleton<ICallerIdentityProvider>(callerIdentityProvider);
    }

    #endregion
}
