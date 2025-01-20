using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// An implementation of <see cref="IPermission"/> using Microsoft Identity.
/// </summary>
public abstract class MicrosoftIdentityPermission : IPermission
{
    /// <summary>
    /// Gets the configuration section name to configure this <see cref="IPermission"/>.
    /// </summary>
    protected abstract string ConfigSectionName { get; }

    /// <summary>
    /// Gets the JWT bearer token scheme.
    /// </summary>
    public abstract string JwtBearerScheme { get; }

    /// <summary>
    /// Add Authentication to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
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
    /// Add <see cref="IPermissionPolicy"/> to the <see cref="IPoliciesBuilder"/>.
    /// </summary>
    /// <param name="policiesBuilder">The <see cref="IPoliciesBuilder"/> to add the policies to the permission.</param>
    public abstract void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    /// <summary>
    /// Gets the required audience of the JWT bearer token.
    /// </summary>
    public string GetAudience(
        IConfiguration configuration)
    {
        var audience = configuration.GetSection(ConfigSectionName).GetValue<string>("Audience");
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new ConfigurationErrorsException($"{ConfigSectionName}:Audience");
        }

        return audience;
    }

    /// <summary>
    /// Gets the required scope of the JWT bearer token.
    /// </summary>
    public string GetScope(
        IConfiguration configuration)
    {
        var scope = configuration.GetSection(ConfigSectionName).GetValue<string>("Scope");
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ConfigurationErrorsException($"{ConfigSectionName}:Scope");
        }

        return scope;
    }
}
