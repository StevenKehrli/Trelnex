using System.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Trelnex.Core.Api.Authentication;

/// <summary>
/// An implementation of <see cref="IPermission"/> using JWT bearer token.
/// </summary>
public abstract class JwtBearerPermission : IPermission
{
    protected abstract string ConfigSectionName { get; }

    public abstract string JwtBearerScheme { get; }

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

    public abstract void AddAuthorization(
        IPoliciesBuilder policiesBuilder);

    public string GetAudience(
        IConfiguration configuration)
    {
        var audience = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("Audience")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:Audience");

        return audience;
    }

    public string GetScope(
        IConfiguration configuration)
    {
        var scope = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("Scope")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:Scope");

        return scope;
    }

    private string GetAuthority(
        IConfiguration configuration)
    {
        var authority = configuration
            .GetSection(ConfigSectionName)
            .GetValue<string>("Authority")
            ?? throw new ConfigurationErrorsException($"{ConfigSectionName}:Authority");

        return authority;
    }

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
