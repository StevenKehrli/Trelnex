using Trelnex.Auth.Amazon.Endpoints.JWT;
using Trelnex.Auth.Amazon.Endpoints.RBAC;
using Trelnex.Auth.Amazon.Endpoints.Token;
using Trelnex.Auth.Amazon.Services;
using Trelnex.Core.Amazon.Identity;
using Trelnex.Core.Api;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Swagger;

// Start the authentication service application using the core Application framework.
Application.Run(args, AuthApplication.AddApplication, AuthApplication.UseApplication);

/// <summary>
/// Provides application configuration and startup functionality for the Trelnex Authentication Service.
/// </summary>
/// <remarks>
/// This class contains methods for configuring and initializing the authentication service,
/// including service registration, authentication setup, and endpoint mapping.
/// It serves as the entry point for the application's configuration pipeline.
/// </remarks>
internal static class AuthApplication
{
    #region Public Static Methods

    /// <summary>
    /// Configures services for the authentication application.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="bootstrapLogger">The logger for recording initialization information.</param>
    /// <remarks>
    /// This method adds required services to the dependency injection container:
    /// - Authentication services with JWT bearer configuration
    /// - RBAC permission policies
    /// - Swagger documentation
    /// - AWS identity services
    /// - Application-specific services
    /// </remarks>
    public static void AddApplication(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        // Add authentication services with JWT bearer configuration and RBAC permission policies.
        services
            .AddAuthentication(configuration)
            .AddPermissions(bootstrapLogger);

        // Add Swagger documentation, AWS identity services, and application-specific services.
        services
            .AddSwaggerToServices()
            .AddAmazonIdentity(
                configuration,
                bootstrapLogger)
            .AddServices(
                configuration,
                bootstrapLogger);
    }

    /// <summary>
    /// Configures the application request handling pipeline.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <remarks>
    /// This method sets up the middleware pipeline and maps all endpoints
    /// for the authentication service, including OpenID Connect discovery
    /// endpoints, RBAC management endpoints, and token issuance endpoints.
    /// </remarks>
    public static void UseApplication(
        WebApplication app)
    {
        // Add Swagger to the web application and configure endpoints.
        app
            .AddSwaggerToWebApplication()
            .UseEndpoints();
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Adds RBAC permission policies to the authentication system.
    /// </summary>
    /// <param name="permissionsBuilder">The builder for configuring permissions.</param>
    /// <param name="bootstrapLogger">The logger for recording initialization information.</param>
    /// <returns>The updated permissions builder for method chaining.</returns>
    /// <remarks>
    /// This method registers the RBAC permission policies defined in <see cref="RBACPermission"/>
    /// with the authentication system, enabling role-based access control for endpoints.
    /// </remarks>
    private static IPermissionsBuilder AddPermissions(
        this IPermissionsBuilder permissionsBuilder,
        ILogger bootstrapLogger)
    {
        // Add RBAC permissions using the RBACPermission class.
        permissionsBuilder
            .AddPermissions<RBACPermission>(bootstrapLogger);

        return permissionsBuilder;
    }

    /// <summary>
    /// Maps all endpoints for the authentication service.
    /// </summary>
    /// <param name="erb">The endpoint route builder for configuring routes.</param>
    /// <returns>The updated endpoint route builder for method chaining.</returns>
    /// <remarks>
    /// This method maps all API endpoints for the authentication service, organized by category:
    ///
    /// 1. OpenID Connect Discovery endpoints:
    ///    - /.well-known/jwks.json for the JSON Web Key Set
    ///    - /.well-known/openid-configuration for the OpenID Connect configuration
    ///
    /// 2. RBAC Management endpoints:
    ///    - Principal Memberships (get, grant, revoke)
    ///    - Principals (delete)
    ///    - Resources (create, delete, get)
    ///    - Role Assignments (get)
    ///    - Roles (create, delete, get)
    ///    - Scopes (create, delete, get)
    ///
    /// 3. Token issuance endpoint:
    ///    - OAuth 2.0 token endpoint for client credentials flow
    /// </remarks>
    private static IEndpointRouteBuilder UseEndpoints(
        this IEndpointRouteBuilder erb)
    {
        // OpenID Connect discovery endpoints.
        GetJsonWebKeySetEndpoint.Map(erb);
        GetOpenIdConfigurationEndpoint.Map(erb);

        // RBAC - principals.
        DeletePrincipalEndpoint.Map(erb);
        GetPrincipalAccessEndpoint.Map(erb);

        // RBAC - resources.
        CreateResourceEndpoint.Map(erb);
        DeleteResourceEndpoint.Map(erb);
        GetResourceEndpoint.Map(erb);

        // RBAC - role assignments.
        CreateRoleAssignmentEndpoint.Map(erb);
        DeleteRoleAssignmentEndpoint.Map(erb);
        GetRoleAssignmentEndpoint.Map(erb);

        // RBAC - roles.
        CreateRoleEndpoint.Map(erb);
        DeleteRoleEndpoint.Map(erb);
        GetRoleEndpoint.Map(erb);

        // RBAC - scope assignments.
        CreateScopeAssignmentEndpoint.Map(erb);
        DeleteScopeAssignmentEndpoint.Map(erb);
        GetRoleAssignmentEndpoint.Map(erb);

        // RBAC - scopes.
        CreateScopeEndpoint.Map(erb);
        DeleteScopeEndpoint.Map(erb);
        GetScopeEndpoint.Map(erb);

        // Token issuance endpoint.
        GetTokenEndpoint.Map(erb);

        return erb;
    }

    #endregion
}
