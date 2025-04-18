using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Auth.Amazon.Tests.Authentication;

internal class TestPermission1 : IPermission
{
    /// <summary>
    /// Gets the JWT bearer token scheme.
    /// </summary>
    public string JwtBearerScheme => "Bearer.trelnex-auth-amazon-tests-authentication-1";

    /// <summary>
    /// Add Authentication to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
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
                        ValidIssuer =  "Issuer.trelnex-auth-amazon-tests-authentication-1",

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
        return "Audience.trelnex-auth-amazon-tests-authentication-1";
    }

    /// <summary>
    /// Gets the required scope of the JWT bearer token.
    /// </summary>
    public string GetScope(
        IConfiguration configuration)
    {
        return "Scope.trelnex-auth-amazon-tests-authentication-1";
    }

    public class TestRolePolicy : IPermissionPolicy
    {
        public string[] RequiredRoles => [ "test.role.1" ];
    }
}
