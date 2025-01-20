using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for a permission.
/// </summary>
/// <remarks>
/// A permission protects an endpoint with its Authentication and Authorization.
/// The Authorization is defined by the permission policies of this object.
/// </remarks>
public interface IPermission
{
    /// <summary>
    /// Gets the JWT bearer token scheme.
    /// </summary>
    public string JwtBearerScheme { get; }

    /// <summary>
    /// Add Authentication to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration);

    /// <summary>
    /// Add <see cref="IPermissionPolicy"/> to the <see cref="IPoliciesBuilder"/>.
    /// </summary>
    /// <param name="policiesBuilder">The <see cref="IPoliciesBuilder"/> to add the policies to the permission.</param>
    public void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    /// <summary>
    /// Gets the required audience of the JWT bearer token.
    /// </summary>
    public string GetAudience(
        IConfiguration configuration);

    /// <summary>
    /// Gets the required scope of the JWT bearer token.
    /// </summary>
    public string GetScope(
        IConfiguration configuration);
}
