using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Defines the contract for implementing API endpoint protection through authentication and authorization.
/// </summary>
/// <remarks>
/// A permission represents a security configuration that:
/// <list type="bullet">
///   <item>Defines how API endpoints are protected</item>
///   <item>Configures the authentication scheme</item>
///   <item>Specifies authorization policies and requirements</item>
///   <item>Manages JWT token validation parameters</item>
/// </list>
/// Implementations should provide cohesive security configurations that can be
/// reused across multiple endpoints with similar protection requirements.
/// </remarks>
public interface IPermission
{
    /// <summary>
    /// Gets the JWT bearer token authentication scheme name.
    /// </summary>
    /// <value>
    /// A string identifier for the JWT bearer token scheme used by this permission.
    /// </value>
    /// <remarks>
    /// This scheme name is used to identify the authentication method when applying
    /// the [Authorize] attribute to controllers or actions.
    /// </remarks>
    public string JwtBearerScheme { get; }

    /// <summary>
    /// Configures authentication services for this permission.
    /// </summary>
    /// <param name="services">The service collection to register authentication services with.</param>
    /// <param name="configuration">The application configuration containing auth settings.</param>
    /// <remarks>
    /// Implementations should register appropriate authentication handlers and
    /// configure JWT validation parameters according to their specific requirements.
    /// </remarks>
    public void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration);

    /// <summary>
    /// Configures authorization policies for this permission.
    /// </summary>
    /// <param name="policiesBuilder">The builder for registering permission-specific authorization policies.</param>
    /// <remarks>
    /// Implementations should define authorization policies that enforce the security
    /// requirements represented by this permission. These policies can include
    /// role requirements, scope validation, and custom authorization rules.
    /// </remarks>
    public void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    /// <summary>
    /// Gets the required audience value for JWT token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing audience settings.</param>
    /// <returns>The audience string that tokens must contain to be considered valid.</returns>
    /// <remarks>
    /// The audience represents the intended recipient of the JWT token and is used
    /// during token validation to ensure tokens are used by their intended recipient.
    /// </remarks>
    public string GetAudience(
        IConfiguration configuration);

    /// <summary>
    /// Gets the required scope value for JWT token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing scope settings.</param>
    /// <returns>The scope string that tokens must contain to be considered valid.</returns>
    /// <remarks>
    /// The scope represents the permissions granted by the token and is used
    /// during token validation to ensure tokens have the required permissions.
    /// </remarks>
    public string GetScope(
        IConfiguration configuration);
}
