using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the builder interface for registering and configuring permissions in the application.
/// </summary>
/// <remarks>
/// The permissions builder provides a fluent API for registering permissions and their associated
/// authorization policies. Each permission defines an authentication scheme and authorization
/// requirements that can be applied to API endpoints.
///
/// This pattern allows for declarative registration of security requirements that can be
/// applied consistently across different parts of the application.
/// </remarks>
public interface IPermissionsBuilder
{
    /// <summary>
    /// Registers a permission type with the application.
    /// </summary>
    /// <typeparam name="T">The permission type to register.</typeparam>
    /// <param name="bootstrapLogger">Logger to record permission registration details.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// This method:
    /// <list type="bullet">
    ///   <item>Creates an instance of the permission type</item>
    ///   <item>Configures authentication for that permission</item>
    ///   <item>Registers authorization policies defined by the permission</item>
    ///   <item>Records detailed configuration in the logs</item>
    /// </list>
    /// Multiple permissions can be registered by chaining calls to this method.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the permission type cannot be instantiated.
    /// </exception>
    public IPermissionsBuilder AddPermissions<T>(
        ILogger bootstrapLogger)
        where T : IPermission;
}

/// <summary>
/// Implementation of the permissions builder that registers and configures permissions.
/// </summary>
/// <remarks>
/// This class handles the registration of permissions and their associated authorization policies,
/// binding them to the application's security provider and authorization system.
/// </remarks>
internal class PermissionsBuilder : IPermissionsBuilder
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;
    private readonly SecurityProvider _securityProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionsBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection to register authorization policies with.</param>
    /// <param name="configuration">The application configuration containing authentication settings.</param>
    /// <param name="securityProvider">The security provider to store security definitions and requirements.</param>
    /// <remarks>
    /// The permissions builder uses the security provider to maintain a registry of all
    /// security definitions and requirements in the application, which are then used
    /// for both authorization and API documentation (Swagger).
    /// </remarks>
    internal PermissionsBuilder(
        IServiceCollection services,
        IConfiguration configuration,
        SecurityProvider securityProvider)
    {
        _services = services;
        _configuration = configuration;
        _securityProvider = securityProvider;
    }

    /// <summary>
    /// Registers a permission type with the application.
    /// </summary>
    /// <typeparam name="T">The permission type to register.</typeparam>
    /// <param name="bootstrapLogger">Logger to record permission registration details.</param>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// This method performs the complete registration workflow for a permission:
    /// <list type="number">
    ///   <item>Creates an instance of the permission</item>
    ///   <item>Configures authentication for the permission scheme</item>
    ///   <item>Extracts security parameters (audience, scope)</item>
    ///   <item>Creates a security definition and adds it to the security provider</item>
    ///   <item>Collects authorization policies from the permission</item>
    ///   <item>Registers all security requirements with detailed logging</item>
    ///   <item>Adds all authorization policies to the service collection</item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the permission type cannot be instantiated.
    /// </exception>
    public IPermissionsBuilder AddPermissions<T>(
        ILogger bootstrapLogger) where T : IPermission
    {
        var permission = Activator.CreateInstance<T>();

        // Configure authentication for this permission
        permission.AddAuthentication(_services, _configuration);

        // Get the security parameters from the permission
        var audience = permission.GetAudience(_configuration);
        var scope = permission.GetScope(_configuration);

        // Create and register the security definition
        var securityDefinition = new SecurityDefinition(
            jwtBearerScheme: permission.JwtBearerScheme,
            audience: audience,
            scope: scope);

        _securityProvider.AddSecurityDefinition(securityDefinition);

        // Configure authorization policies for this permission
        var policiesBuilder = new PoliciesBuilder(_securityProvider, securityDefinition);
        permission.AddAuthorization(policiesBuilder);

        // Log all security requirements for this permission
        var securityRequirements = _securityProvider.GetSecurityRequirements(
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

            // Log using literal format (:l) to avoid quotes in the output
            bootstrapLogger.LogInformation(
                message: "Added Policy '{policyName:l}' to Permission '{permissionName:l}': jwtBearerScheme = '{jwtBearerScheme:l}'; audience = '{audience:l}'; scope = '{scope:l}'; requiredRoles = '{requiredRoles:l}'.",
                args: args);
        }

        // Register all policies with ASP.NET Core authorization
        _services.AddAuthorization(policiesBuilder.Build);

        return this;
    }
}
