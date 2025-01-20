using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a permissions builder.
/// </summary>
/// <remarks>
/// A permissions builder collects the permissions through its <see cref="IPermissionsBuilder.AddPermissions{T}"/>.
/// </remarks>
public interface IPermissionsBuilder
{
    /// <summary>
    /// Adds the specified permission to this builder.
    /// </summary>
    /// <typeparam name="T">The specified <see cref="IPermission"/>.</typeparam>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <returns>This builder</returns>
    public IPermissionsBuilder AddPermissions<T>(
        ILogger bootstrapLogger)
        where T : IPermission;
}

/// <summary>
/// Initializes a new instance of the <see cref="PermissionsBuilder"/>.
/// </summary>
/// <param name="services">The <see cref="IServiceCollection"/> to add the authorization policy services to.</param>
/// <param name="configuration">Represents a set of key/value application configuration properties.</param>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/> to add the security definition of the permission.</param>
internal class PermissionsBuilder(
    IServiceCollection services,
    IConfiguration configuration,
    SecurityProvider securityProvider) : IPermissionsBuilder
{
    /// <summary>
    /// Adds the specified permission to this builder.
    /// </summary>
    /// <typeparam name="T">The specified <see cref="IPermission"/>.</typeparam>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the CommandProvider bootstrap logs.</param>
    /// <returns>This builder</returns>
    public IPermissionsBuilder AddPermissions<T>(
        ILogger bootstrapLogger) where T : IPermission
    {
        var permission = Activator.CreateInstance<T>();

        permission.AddAuthentication(services, configuration);

        // get the security definition
        var audience = permission.GetAudience(configuration);
        var scope = permission.GetScope(configuration);

        var securityDefinition = new SecurityDefinition(
            jwtBearerScheme: permission.JwtBearerScheme,
            audience: audience,
            scope: scope);

        securityProvider.AddSecurityDefinition(securityDefinition);

        // get the policies builder
        var policiesBuilder = new PoliciesBuilder(securityProvider, securityDefinition);
        permission.AddAuthorization(policiesBuilder);

        // get the security requirements from the security provider
        var securityRequirements = securityProvider.GetSecurityRequirements(
            jwtBearerScheme: permission.JwtBearerScheme,
            audience: audience,
            scope: scope);

        foreach (var securityRequirement in securityRequirements)
        {
            object[] args =
            [
                securityRequirement.Policy, // policyName
                typeof(T), // permissionName
                permission.JwtBearerScheme, // jwtBearerScheme
                audience, // audience
                scope, // scope
                securityRequirement.RequiredRoles // requiredRoles
            ];

            // log - the :l format parameter (l = literal) to avoid the quotes
            bootstrapLogger.LogInformation(
                message: "Added Policy '{policyName:l}' to Permission '{permissionName:l}': jwtBearerScheme = '{jwtBearerScheme:l}'; audience = '{audience:l}'; scope = '{scope:l}'; requiredRoles = '{requiredRoles:l}'.",
                args: args);
        }

        services.AddAuthorization(policiesBuilder.Build);

        return this;
    }
}
