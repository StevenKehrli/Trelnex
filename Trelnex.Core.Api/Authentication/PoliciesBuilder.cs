using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a permission policies builder.
/// </summary>
/// <remarks>
/// Implements a fluent interface to configure authorization policies.
/// </remarks>
public interface IPoliciesBuilder
{
    /// <summary>
    /// Adds a specified permission policy to the builder's collection.
    /// </summary>
    /// <typeparam name="T">The type of permission policy to add.</typeparam>
    /// <returns>The same builder instance for method chaining.</returns>
    public IPoliciesBuilder AddPolicy<T>() where T : IPermissionPolicy;
}

/// <summary>
/// Implements the permission policies builder for configuring authorization policies.
/// </summary>
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
    public IPoliciesBuilder AddPolicy<T>() where T : IPermissionPolicy
    {
        // Get the policy name from the permission policy type.
        var policyName = PermissionPolicy.Name<T>();

        // Create an instance of the permission policy.
        var policy = Activator.CreateInstance<T>();

        // Add the policy to the list of policy containers.
        _policyContainers.Add(
            new PolicyContainer(policyName, policy)
        );

        // Create a security requirement for the policy.
        var securityRequirement =
            new SecurityRequirement(
                jwtBearerScheme: securityDefinition.JwtBearerScheme,
                audience: securityDefinition.Audience,
                scope: securityDefinition.Scope,
                policy: policyName,
                requiredRoles: policy.RequiredRoles);

        // Register the security requirement with the security provider.
        securityProvider.AddSecurityRequirement(securityRequirement);

        return this;
    }

    /// <summary>
    /// Builds and registers all configured policies with the authorization options.
    /// </summary>
    /// <param name="options">The authorization options to configure with these policies.</param>
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
    private record PolicyContainer(
        string Name,
        IPermissionPolicy Policy);
}
