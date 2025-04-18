using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Auth.Amazon.Tests.Authentication;

internal class TestPermission : IPermission
{
    /// <summary>
    /// Gets the JWT bearer token scheme.
    /// </summary>
    public string JwtBearerScheme => "Bearer.trelnex-auth-amazon-tests-authentication";

    /// <summary>
    /// Add Authentication to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public void AddAuthentication(
        IServiceCollection services,
        IConfiguration configuration)
    {
        IdentityModelEventSource.ShowPII = true;

        services
            .AddAuthentication()
            .AddJwtBearer(
                JwtBearerScheme,
                options =>
                {
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
                        ValidIssuer =  "Issuer.trelnex-auth-amazon-tests-authentication",

                        IssuerSigningKey = TestAlgorithm.SecurityKey,
                    };
                });
    }

    /// <summary>
    /// Add <see cref="IPermissionPolicy"/> to the <see cref="IPoliciesBuilder"/>.
    /// </summary>
    /// <param name="policiesBuilder">The <see cref="IPoliciesBuilder"/> to add the policies to the permission.</param>
    public void AddAuthorization(
        IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<TestRolePolicy>();
    }

    /// <summary>
    /// Gets the required audience of the JWT bearer token.
    /// </summary>
    public string GetAudience(
        IConfiguration configuration)
    {
        return "Audience.trelnex-auth-amazon-tests-authentication";
    }

    /// <summary>
    /// Gets the required scope of the JWT bearer token.
    /// </summary>
    public string GetScope(
        IConfiguration configuration)
    {
        return "Scope.trelnex-auth-amazon-tests-authentication";
    }

    public class TestRolePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => [ "test.role" ];
    }
}
