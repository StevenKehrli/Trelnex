using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// Base implementation of <see cref="IPermission"/> that configures JWT Bearer token authentication.
/// </summary>
/// <remarks>
/// This abstract class provides core JWT Bearer token authentication functionality and
/// requires derived classes to implement specific authorization policies and configuration section details.
///
/// Configuration requires the following settings in the specified configuration section:
/// <list type="bullet">
///   <item><c>Authority</c>: The issuer URL of the identity provider</item>
///   <item><c>Audience</c>: The valid audience for the JWT token</item>
///   <item><c>MetadataAddress</c>: The URL to the OAuth/OpenID Connect metadata document</item>
///   <item><c>Scope</c>: The required scope value for authorization</item>
/// </list>
/// </remarks>
public abstract class JwtBearerPermission : IPermission
{
    /// <summary>
    /// Gets the configuration section name where JWT Bearer settings are stored.
    /// </summary>
    /// <value>
    /// The name of the configuration section containing JWT settings.
    /// </value>
    /// <remarks>
    /// Derived classes must specify which configuration section contains the required
    /// JWT Bearer token settings (Authority, Audience, MetadataAddress, Scope).
    /// </remarks>
    protected abstract string ConfigSectionName { get; }

    /// <summary>
    /// Gets the JWT Bearer authentication scheme name.
    /// </summary>
    /// <value>
    /// The scheme name that identifies this JWT Bearer authentication handler.
    /// </value>
    /// <remarks>
    /// This value is used when registering the JWT Bearer authentication handler
    /// and when applying the [Authorize] attribute with a specific scheme.
    /// </remarks>
    public abstract string JwtBearerScheme { get; }

    /// <summary>
    /// Configures JWT Bearer token authentication for this permission.
    /// </summary>
    /// <param name="services">The service collection to register authentication services with.</param>
    /// <param name="configuration">The application configuration containing JWT settings.</param>
    /// <remarks>
    /// Configures the JWT Bearer token handler with settings from the configuration section
    /// specified by <see cref="ConfigSectionName"/>. Sets up token validation parameters
    /// with secure defaults and values from configuration.
    /// </remarks>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when required configuration values are missing.
    /// </exception>
    public void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddAuthentication()
            .AddJwtBearer(
                JwtBearerScheme,
                options =>
                {
                    options.Authority = GetAuthority(configuration);
                    options.Audience = GetAudience(configuration);
                    options.MetadataAddress = GetMetadataAddress(configuration);

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        RequireAudience = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,

                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,

                        ValidateAudience = true,
                        ValidAudience = GetAudience(configuration),

                        ValidateIssuer = true,
                        ValidIssuer = GetAuthority(configuration),
                    };
                });
    }

    /// <summary>
    /// Configures authorization policies for this permission.
    /// </summary>
    /// <param name="policiesBuilder">The builder for registering authorization policies.</param>
    /// <remarks>
    /// Derived classes must implement this method to define the specific authorization
    /// requirements associated with this permission, such as required scopes or roles.
    /// </remarks>
    public abstract void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    /// <summary>
    /// Gets the required audience value for JWT token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing audience settings.</param>
    /// <returns>The audience string that tokens must contain to be considered valid.</returns>
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
    /// Gets the required scope value for JWT token validation.
    /// </summary>
    /// <param name="configuration">The application configuration containing scope settings.</param>
    /// <returns>The scope string that tokens must contain to be considered valid.</returns>
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

    /// <summary>
    /// Gets the authority (issuer) URL from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The authority URL string.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the Authority configuration value is missing.
    /// </exception>
    private string GetAuthority(
        IConfiguration configuration)
    {
        var authority = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("Authority")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:Authority");

        return authority;
    }

    /// <summary>
    /// Gets the metadata address URL from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The metadata address URL string.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the MetadataAddress configuration value is missing.
    /// </exception>
    private string GetMetadataAddress(
        IConfiguration configuration)
    {
        var metadataAddress = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("MetadataAddress")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:MetadataAddress");

        return metadataAddress;
    }
}
