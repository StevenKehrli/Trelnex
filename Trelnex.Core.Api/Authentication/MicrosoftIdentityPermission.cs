using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Base implementation of <see cref="IPermission"/> that uses Microsoft Identity for authentication.
/// </summary>
/// <remarks>
/// This abstract class configures authentication using Microsoft Identity Web,
/// which simplifies integration with Azure Active Directory and the Microsoft identity platform.
///
/// Configuration requires the following settings in the specified configuration section:
/// <list type="bullet">
///   <item><c>Authority</c>: The Azure AD instance URL.</item>
///   <item><c>Domain</c>: The Azure AD domain name.</item>
///   <item><c>TenantId</c>: The Azure AD tenant ID.</item>
///   <item><c>ClientId</c>: The application's client ID (app registration ID).</item>
///   <item><c>Audience</c>: The valid audience for the JWT token.</item>
///   <item><c>Scope</c>: The required scope value for authorization.</item>
/// </list>
/// </remarks>
public abstract class MicrosoftIdentityPermission : IPermission
{
    #region Public Abstract Properties

    /// <summary>
    /// Gets the JWT Bearer authentication scheme name.
    /// </summary>
    /// <value>The scheme name that identifies this Microsoft Identity authentication handler.</value>
    /// <remarks>
    /// This value is used when registering the JWT Bearer authentication handler
    /// and when applying the <see cref="AuthorizeAttribute"/> with a specific scheme.
    /// </remarks>
    public abstract string JwtBearerScheme { get; }

    #endregion

    #region Protected Abstract Properties

    /// <summary>
    /// Gets the configuration section name where Microsoft Identity settings are stored.
    /// </summary>
    /// <value>The name of the configuration section containing Microsoft Identity settings.</value>
    /// <remarks>
    /// Derived classes must specify which configuration section contains the required
    /// Microsoft Identity Web settings. This typically follows the format "AzureAd"
    /// or a similar descriptive name.
    /// </remarks>
    protected abstract string ConfigSectionName { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Configures Microsoft Identity authentication for this permission.
    /// </summary>
    /// <param name="services">The service collection to register authentication services with.</param>
    /// <param name="configuration">The application configuration containing Azure AD settings.</param>
    /// <remarks>
    /// Uses Microsoft Identity Web to configure authentication with Azure Active Directory.
    /// The configuration is loaded from the section specified by <see cref="ConfigSectionName"/>.
    /// </remarks>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when required configuration values are missing.
    /// </exception>
    public void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMicrosoftIdentityWebApiAuthentication(
            configuration,
            ConfigSectionName,
            JwtBearerScheme);
    }

    /// <summary>
    /// Gets the required audience value for token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing Azure AD settings.</param>
    /// <returns>The audience string that tokens must contain to be considered valid.</returns>
    /// <remarks>
    /// For Microsoft Identity, this is typically the application ID URI or client ID.
    /// </remarks>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the Audience configuration value is missing.
    /// </exception>
    public string GetAudience(
        IConfiguration configuration)
    {
        var audience = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("Audience")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:Audience");

        return audience;
    }

    /// <summary>
    /// Gets the required scope value for token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing Azure AD settings.</param>
    /// <returns>The scope string that tokens must contain to be considered valid.</returns>
    /// <remarks>
    /// For Microsoft Identity, this typically follows the format "api://{clientId}/scope-name".
    /// </remarks>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the Scope configuration value is missing.
    /// </exception>
    public string GetScope(
        IConfiguration configuration)
    {
        var scope = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("Scope")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:Scope");

        return scope;
    }

    #endregion

    #region Public Abstract Methods

    /// <summary>
    /// Configures authorization policies for this permission.
    /// </summary>
    /// <param name="policiesBuilder">The builder for registering authorization policies.</param>
    /// <remarks>
    /// Derived classes must implement this method to define the specific authorization
    /// requirements associated with this permission, typically including Azure AD role
    /// or scope-based authorization requirements.
    /// </remarks>
    public abstract void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    #endregion
}
