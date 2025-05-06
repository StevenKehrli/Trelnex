using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a permission policies builder that configures authorization policies.
/// </summary>
/// <remarks>
/// The permission policies builder implements a fluent interface pattern to collect and configure
/// strongly-typed permission policies. Each policy represents a specific authorization requirement
/// that can be applied to API endpoints.
///
/// The builder pattern allows for a clean, readable configuration syntax in application startup.
/// </remarks>
public interface IPoliciesBuilder
{
    /// <summary>
    /// Adds a specified permission policy to the builder's collection.
    /// </summary>
    /// <typeparam name="T">The type of permission policy to add, implementing <see cref="IPermissionPolicy"/>.</typeparam>
    /// <returns>The same builder instance for method chaining.</returns>
    /// <remarks>
    /// This method enables the fluent configuration of multiple permission policies:
    ///
    /// <code>
    /// services.AddAuthentication(configuration)
    ///     .AddPolicy&lt;ReadDataPermission&gt;()
    ///     .AddPolicy&lt;WriteDataPermission&gt;()
    ///     .AddPolicy&lt;AdminPermission&gt;();
    /// </code>
    /// </remarks>
    public IPoliciesBuilder AddPolicy<T>() where T : IPermissionPolicy;
}

/// <summary>
/// Implements the permission policies builder for configuring authorization policies.
/// </summary>
/// <remarks>
/// This class handles the registration and configuration of authorization policies based on
/// strongly-typed permission policy definitions. It manages the association between policies,
/// security requirements, and role-based permissions.
/// </remarks>
/// <param name="securityProvider">The security provider to register security requirements.</param>
/// <param name="securityDefinition">The security definition containing authentication scheme information.</param>
internal class PoliciesBuilder(
    SecurityProvider securityProvider,
    ISecurityDefinition securityDefinition) : IPoliciesBuilder
{
    private readonly List<PolicyContainer> _policyContainers = [];

    /// <summary>
    /// Adds a permission policy to the builder and registers its security requirements.
    /// </summary>
    /// <typeparam name="T">The type of permission policy to add.</typeparam>
    /// <returns>The builder instance for method chaining.</returns>
    /// <remarks>
    /// This method performs several key operations:
    /// <list type="number">
    ///   <item>Creates a unique policy name based on the policy type</item>
    ///   <item>Instantiates the policy to access its required roles</item>
    ///   <item>Creates a security requirement with appropriate audience, scope, and roles</item>
    ///   <item>Registers the requirement with the security provider for use in Swagger documentation</item>
    /// </list>
    ///
    /// The security requirements defined here will be used both for actual authorization
    /// enforcement and for API documentation through Swagger.
    /// </remarks>
    public IPoliciesBuilder AddPolicy<T>() where T : IPermissionPolicy
    {
        var policyName = PermissionPolicy.Name<T>();

        var policy = Activator.CreateInstance<T>();

        _policyContainers.Add(
            new PolicyContainer(policyName, policy)
        );

        var securityRequirement =
            new SecurityRequirement(
                jwtBearerScheme: securityDefinition.JwtBearerScheme,
                audience: securityDefinition.Audience,
                scope: securityDefinition.Scope,
                policy: policyName,
                requiredRoles: policy.RequiredRoles);

        securityProvider.AddSecurityRequirement(securityRequirement);

        return this;
    }

    /// <summary>
    /// Builds and registers all configured policies with the authorization options.
    /// </summary>
    /// <param name="options">The authorization options to configure with these policies.</param>
    /// <remarks>
    /// This method configures each policy with:
    /// <list type="bullet">
    ///   <item>The appropriate authentication scheme (from the security definition)</item>
    ///   <item>The required scope claim (from the security definition)</item>
    ///   <item>The required roles specified by each policy</item>
    /// </list>
    ///
    /// These configured policies can then be applied to API endpoints using the
    /// <see cref="PermissionsExtensions.RequirePermission{T}"/> extension method.
    /// </remarks>
    internal void Build(
        AuthorizationOptions options)
    {
        _policyContainers.ForEach(policyContainer =>
        {
            options.AddPolicy(
                policyContainer.Name,
                policyBuilder =>
                {
                    policyBuilder.AuthenticationSchemes = [securityDefinition.JwtBearerScheme];
                    policyBuilder.RequireClaim(ClaimConstants.Scope, securityDefinition.Scope);
                    Array.ForEach(
                        policyContainer.Policy.RequiredRoles,
                        r => policyBuilder.RequireRole(r));
                });
        });
    }

    /// <summary>
    /// Represents a named permission policy container with its policy implementation.
    /// </summary>
    /// <param name="Name">The policy name used for registration and lookup.</param>
    /// <param name="Policy">The policy implementation defining required roles.</param>
    /// <remarks>
    /// This record provides a convenient way to group policy names with their implementations
    /// for use during the authorization configuration process.
    /// </remarks>
    private record PolicyContainer(
        string Name,
        IPermissionPolicy Policy);
}
