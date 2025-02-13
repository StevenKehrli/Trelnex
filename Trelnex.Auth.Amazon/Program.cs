using Trelnex.Auth.Amazon.Endpoints.JWT;
using Trelnex.Auth.Amazon.Endpoints.RBAC;
using Trelnex.Auth.Amazon.Endpoints.Token;
using Trelnex.Auth.Amazon.Services;
using Trelnex.Core.Amazon.Identity;
using Trelnex.Core.Api;
using Trelnex.Core.Api.Authentication;
using Trelnex.Core.Api.Swagger;

Application.Run(args, AuthApplication.AddApplication, AuthApplication.UseApplication);

internal static class AuthApplication
{
    public static void AddApplication(
        IServiceCollection services,
        IConfiguration configuration,
        ILogger bootstrapLogger)
    {
        services
            .AddAuthentication(configuration)
            .AddPermissions(bootstrapLogger);

        services
            .AddSwaggerToServices()
            .AddAmazonIdentity(
                configuration,
                bootstrapLogger)
            .AddServices(
                configuration,
                bootstrapLogger);
    }

    public static void UseApplication(
        WebApplication app)
    {
        app
            .AddSwaggerToWebApplication()
            .UseEndpoints();
    }

    private static IPermissionsBuilder AddPermissions(
        this IPermissionsBuilder permissionsBuilder,
        ILogger bootstrapLogger)
    {
        permissionsBuilder
            .AddPermissions<RBACPermission>(bootstrapLogger);

        return permissionsBuilder;
    }

    private static IEndpointRouteBuilder UseEndpoints(
        this IEndpointRouteBuilder erb)
    {
        // openId
        GetJsonWebKeySetEndpoint.Map(erb);
        GetOpenIdConfigurationEndpoint.Map(erb);

        // rbac - principal memberships
        GetPrincipalMembershipEndpoint.Map(erb);
        GrantPrincipalMembershipEndpoint.Map(erb);
        RevokePrincipalMembershipEndpoint.Map(erb);

        // // rbac - principals
        DeletePrincipalEndpoint.Map(erb);

        // rbac - resources
        CreateResourceEndpoint.Map(erb);
        DeleteResourceEndpoint.Map(erb);
        GetResourceEndpoint.Map(erb);

        // rbac - role assignments
        GetRoleAssignmentEndpoint.Map(erb);

        // rbac - roles
        CreateRoleEndpoint.Map(erb);
        DeleteRoleEndpoint.Map(erb);
        GetRoleEndpoint.Map(erb);

        // rbac - scopes
        CreateScopeEndpoint.Map(erb);
        DeleteScopeEndpoint.Map(erb);
        GetScopeEndpoint.Map(erb);

        // token
        GetTokenEndpoint.Map(erb);

        return erb;
    }
}
