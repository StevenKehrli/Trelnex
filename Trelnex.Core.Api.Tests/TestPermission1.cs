using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test implementation of <see cref="IPermission"/> used to configure a specific JWT authentication scheme for testing purposes.
/// It defines the JWT bearer scheme, audience, issuer, and signing key used to validate tokens in tests.
/// This allows tests to simulate different authentication scenarios with specific token requirements.
/// It differs from <see cref="TestPermission2"/> by using a different JWT bearer scheme, audience, issuer, and required role.
/// </summary>
internal class TestPermission1 : IPermission
{
    #region Public Properties

    /// <summary>
    /// Gets the JWT bearer token scheme.
    /// </summary>
    public string JwtBearerScheme => "Bearer.trelnex-auth-amazon-tests-authentication-1";

    #endregion

    #region Public Methods

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
                // Use the defined JWT bearer scheme.
                JwtBearerScheme,
                options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                         // Ensure that the audience is present in the token.
                        RequireAudience = true,
                        // Ensure that the expiration time is present in the token.
                        RequireExpirationTime = true,
                        // Ensure that the token is signed.
                        RequireSignedTokens = true,

                        // Validate the issuer signing key.
                        ValidateIssuerSigningKey = true,
                        // Validate the lifetime of the token.
                        ValidateLifetime = true,

                        // Validate the audience of the token.
                        ValidateAudience = true,
                        // Set the valid audience.
                        ValidAudience = GetAudience(configuration),

                        // Validate the issuer of the token.
                        ValidateIssuer = true,
                        // Set the valid issuer.
                        ValidIssuer =  "Issuer.trelnex-auth-amazon-tests-authentication-1",

                        // Set the issuer signing key.
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
            .AddPolicy<TestRolePolicy>(); // Add the test role policy.
    }

    /// <summary>
    /// Gets the required audience of the JWT bearer token.
    /// </summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The required audience of the JWT bearer token.</returns>
    public string GetAudience(
        IConfiguration configuration)
    {
        return "Audience.trelnex-auth-amazon-tests-authentication-1"; // Return the required audience.
    }

    /// <summary>
    /// Gets the required scope of the JWT bearer token.
    /// </summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <returns>The required scope of the JWT bearer token.</returns>
    public string GetScope(
        IConfiguration configuration)
    {
        return "Scope.trelnex-auth-amazon-tests-authentication-1"; // Return the required scope.
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// A test implementation of <see cref="IPermissionPolicy"/> that requires a specific role.
    /// </summary>
    public class TestRolePolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the required roles for the policy.
        /// </summary>
        public string[] RequiredRoles => [ "test.role.1" ]; // Return the required roles.
    }

    #endregion
}
