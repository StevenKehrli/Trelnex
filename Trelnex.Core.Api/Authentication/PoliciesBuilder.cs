using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a permission policies builder.
/// </summary>
/// <remarks>
/// A permission policies builder collects the permission policies through its <see cref="IPoliciesBuilder.AddPolicy{T}"/>.
/// </remarks>
public interface IPoliciesBuilder
{
    /// <summary>
    /// Adds the specified permission policy to this builder.
    /// </summary>
    /// <typeparam name="T">The specified <see cref="IPermissionPolicy"/>.</typeparam>
    /// <returns>This builder.</returns>
    public IPoliciesBuilder AddPolicy<T>() where T : IPermissionPolicy;
}

/// <summary>
/// Initializes a new instance of the <see cref="PoliciesBuilder"/>.
/// </summary>
/// <param name="securityProvider">The <see cref="ISecurityProvider"/> to add the security requirements of the permission policies.</param>
/// <param name="securityDefinition">The <see cref="ISecurityDefinition"/> that own the permission policies of this builder.</param>
internal class PoliciesBuilder(
    SecurityProvider securityProvider,
    ISecurityDefinition securityDefinition) : IPoliciesBuilder
{
    private readonly List<PolicyContainer> _policyContainers = [];

    /// <summary>
    /// Adds the specified permission policy to this builder.
    /// </summary>
    /// <remarks>
    /// Creates an <see cref="ISecurityRequirement"/> that defines the security requirement of the permission policy
    /// and adds it to the <see cref="ISecurityProvider"/>.
    /// </remarks>
    /// <typeparam name="T">The specified <see cref="IPermissionPolicy"/>.</typeparam>
    /// <returns>This builder.</returns>
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
    /// Adds the permission policies to the <see cref="AuthorizationOptions"/>.
    /// </summary>
    /// <param name="options">The specified <see cref="AuthorizationOptions"/> to add the permission policies to.</param>
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
    /// Represents a permission policy by its name and <see cref="IPermissionPolicy"/>.
    /// </summary>
    /// <param name="Name">The name of the permission policy.</param>
    /// <param name="Policy">The <see cref="IPermissionPolicy"/>.</param>
    private record PolicyContainer(
        string Name,
        IPermissionPolicy Policy);
}
