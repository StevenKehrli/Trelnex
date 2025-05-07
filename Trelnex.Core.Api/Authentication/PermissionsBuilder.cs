using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the builder interface for registering and configuring permissions.
/// </summary>
/// <remarks>
/// Provides a fluent API for registering permissions and their authorization policies.
/// </remarks>
public interface IPermissionsBuilder
{
    /// <summary>
    /// Registers a permission type with the application.
    /// </summary>
    /// <typeparam name="T">The permission type to register.</typeparam>
    /// <param name="bootstrapLogger">Logger to record permission registration details.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    IPermissionsBuilder AddPermissions<T>(
        ILogger bootstrapLogger)
        where T : IPermission;
}

/// <summary>
/// Implementation of the permissions builder that registers and configures permissions.
/// </summary>
/// <param name="services">The service collection to register authorization policies with.</param>
/// <param name="configuration">The application configuration containing authentication settings.</param>
/// <param name="securityProvider">The security provider to store security definitions and requirements.</param>
internal class PermissionsBuilder(
    IServiceCollection services,
    IConfiguration configuration,
    SecurityProvider securityProvider)
    : IPermissionsBuilder
{
    #region Public Methods

    /// <inheritdoc />
    public IPermissionsBuilder AddPermissions<T>(
        ILogger bootstrapLogger) where T : IPermission
    {
        // Create an instance of the permission type.
        var permission = Activator.CreateInstance<T>();

        // Configure authentication settings for this permission.
        permission.AddAuthentication(services, configuration);

        // Retrieve the security parameters from the permission.
        var audience = permission.GetAudience(configuration);
        var scope = permission.GetScope(configuration);

        // Create and register the security definition.
        var securityDefinition = new SecurityDefinition(
            jwtBearerScheme: permission.JwtBearerScheme,
            audience: audience,
            scope: scope);

        securityProvider.AddSecurityDefinition(securityDefinition);

        // Configure authorization policies for this permission.
        var policiesBuilder = new PoliciesBuilder(securityProvider, securityDefinition);
        permission.AddAuthorization(policiesBuilder);

        // Retrieve all security requirements for this permission.
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

            // Log all security requirements for this permission using a literal format to avoid quotes in the output.
            bootstrapLogger.LogInformation(
                message: "Added Policy '{policyName:l}' to Permission '{permissionName:l}': jwtBearerScheme = '{jwtBearerScheme:l}'; audience = '{audience:l}'; scope = '{scope:l}'; requiredRoles = '{requiredRoles:l}'.",
                args: args);
        }

        // Register all policies with ASP.NET Core authorization.
        services.AddAuthorization(policiesBuilder.Build);

        return this;
    }

    #endregion
}
