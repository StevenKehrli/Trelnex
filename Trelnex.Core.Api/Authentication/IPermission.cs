using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for implementing API endpoint protection.
/// </summary>
/// <remarks>
/// Represents a security configuration for API endpoints.
/// </remarks>
public interface IPermission
{
    /// <summary>
    /// Gets the JWT bearer token authentication scheme name.
    /// </summary>
    /// <value>
    /// A string identifier for the JWT bearer token scheme.
    /// </value>
    public string JwtBearerScheme { get; }

    /// <summary>
    /// Configures authentication services for this permission.
    /// </summary>
    /// <param name="services">The service collection to register authentication services with.</param>
    /// <param name="configuration">The application configuration containing auth settings.</param>
    public void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration);

    /// <summary>
    /// Configures authorization policies for this permission.
    /// </summary>
    /// <param name="policiesBuilder">The builder for registering permission-specific authorization policies.</param>
    public void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    /// <summary>
    /// Gets the required audience value for JWT token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing audience settings.</param>
    /// <returns>The audience string that tokens must contain to be considered valid.</returns>
    public string GetAudience(
        IConfiguration configuration);

    /// <summary>
    /// Gets the required scope value for JWT token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing scope settings.</param>
    /// <returns>The scope string that tokens must contain to be considered valid.</returns>
    public string GetScope(
        IConfiguration configuration);
}
