using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test implementation of <see cref="IPermission"/> that configures a specific JWT authentication
/// and authorization scheme for testing role-based access control.
///
/// This class is a key component of the test authentication framework and serves multiple purposes:
///
/// 1. It configures the first of two parallel authentication schemes in the test environment,
///    with its own distinct bearer scheme, audience, issuer, and role requirements.
///
/// 2. It defines the authentication parameters for validating tokens in the first scheme,
///    using the TestAlgorithm's security key for JWT signature validation.
///
/// 3. It provides a nested TestRolePolicy class that specifies the "test.role.1" role requirement
///    used to protect various test endpoints in BaseApiTests.
///
/// 4. It allows AuthenticationTests to verify proper role-based access control by testing
///    endpoints protected with RequirePermission&lt;TestPermission1.TestRolePolicy&gt;.
///
/// The class works in conjunction with TestPermission2 to create a test environment with
/// multiple authorization schemes, allowing tests to verify the correct scheme is enforced
/// for each protected endpoint. This ensures that authentication, audience validation, and
/// role-based access control are properly implemented and enforced.
///
/// TestPermission1 and TestPermission2 each represent a distinct authentication scheme,
/// differentiated by their JwtBearerScheme, Audience, Issuer, and RequiredRoles. This setup allows for comprehensive
/// testing of multi-scheme authentication scenarios.
///
/// - TestPermission1 uses "Bearer.trelnex-auth-amazon-tests-authentication-1" as its JwtBearerScheme,
///   requires the "Audience.trelnex-auth-amazon-tests-authentication-1" audience,
///   expects the "Issuer.trelnex-auth-amazon-tests-authentication-1" issuer,
///   and enforces the "test.role.1" role.
///
/// - TestPermission2 uses "Bearer.trelnex-auth-amazon-tests-authentication-2" as its JwtBearerScheme,
///   requires the "Audience.trelnex-auth-amazon-tests-authentication-2" audience,
///   expects the "Issuer.trelnex-auth-amazon-tests-authentication-2" issuer,
///   and enforces the "test.role.2a" or "test.role.2b" roles.
///
/// Endpoints using this permission require tokens with audience "Audience.trelnex-auth-amazon-tests-authentication-1",
/// issuer "Issuer.trelnex-auth-amazon-tests-authentication-1", and the "test.role.1" role.
/// </summary>
internal class TestPermission1 : IPermission
{
    #region Public Properties

    /// <summary>
    /// Gets the JWT bearer token scheme used for this permission's authentication.
    ///
    /// This unique scheme identifier ("Bearer.trelnex-auth-amazon-tests-authentication-1")
    /// distinguishes this authentication scheme from TestPermission2's scheme, allowing the
    /// test framework to maintain separate authentication configurations and validate tokens
    /// according to different sets of criteria.
    ///
    /// The auth system uses this scheme to determine which validation parameters to apply
    /// when verifying tokens presented to protected endpoints.
    /// </summary>
    public string JwtBearerScheme => "Bearer.trelnex-auth-amazon-tests-authentication-1";

    #endregion

    #region Public Methods

    /// <summary>
    /// Configures JWT Bearer token authentication for this permission's scheme.
    ///
    /// This method sets up JWT Bearer authentication with specific token validation parameters
    /// for the "Bearer.trelnex-auth-amazon-tests-authentication-1" scheme. It configures:
    ///
    /// 1. Audience validation - requiring "Audience.trelnex-auth-amazon-tests-authentication-1"
    /// 2. Issuer validation - requiring "Issuer.trelnex-auth-amazon-tests-authentication-1"
    /// 3. Token signature validation - using TestAlgorithm's security key
    /// 4. Token lifetime validation - ensuring tokens aren't expired
    ///
    /// This configuration ensures that only properly formed tokens with the correct audience,
    /// issuer, and valid signatures can access endpoints protected by this permission.
    /// The test framework uses this to validate behaviors like token rejection when:
    ///  - The token has an incorrect audience
    ///  - The token has an incorrect issuer
    ///  - The token signature is invalid
    ///  - The token is expired
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the authentication services to.</param>
    /// <param name="configuration">Application configuration properties (not used in this implementation).</param>
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

                        // Set the issuer signing key to the same key used for token signing.
                        // This ensures that tokens created by TestJwtProvider can be validated here.
                        IssuerSigningKey = TestAlgorithm.SecurityKey,
                    };
                });
    }

    /// <summary>
    /// Configures authorization policies for this permission by adding the TestRolePolicy.
    ///
    /// This method registers the nested TestRolePolicy with the authorization system, which
    /// requires the "test.role.1" role for access. In BaseApiTests.cs, multiple endpoints are
    /// protected using RequirePermission&lt;TestPermission1.TestRolePolicy&gt;, which enforces
    /// that only tokens containing the "test.role.1" role can access these endpoints.
    ///
    /// The AuthenticationTests class verifies this behavior by testing access to these endpoints
    /// with tokens containing different role combinations, proving that role-based authorization
    /// works correctly.
    /// </summary>
    /// <param name="policiesBuilder">The builder used to register permission policies in the auth system.</param>
    public void AddAuthorization(
        IPoliciesBuilder policiesBuilder)
    {
        policiesBuilder
            .AddPolicy<TestRolePolicy>(); // Add the test role policy.
    }

    /// <summary>
    /// Gets the required audience value for tokens used with this permission.
    ///
    /// The audience "Audience.trelnex-auth-amazon-tests-authentication-1" is a fixed test value
    /// that identifies which service the token is intended for. This is used in two ways:
    ///
    /// 1. When generating test tokens with TestJwtProvider, this value is included as the audience
    ///    claim in tokens that should be valid for endpoints protected by this permission.
    ///
    /// 2. During token validation, the authentication system verifies that the token's audience
    ///    claim matches this value before granting access.
    ///
    /// The test suite uses this to verify audience validation by sending tokens with both correct
    /// and incorrect audience values to protected endpoints.
    /// </summary>
    /// <param name="configuration">Configuration properties (not used in this implementation).</param>
    /// <returns>The fixed audience value "Audience.trelnex-auth-amazon-tests-authentication-1".</returns>
    public string GetAudience(
        IConfiguration configuration)
    {
        return "Audience.trelnex-auth-amazon-tests-authentication-1";
    }

    /// <summary>
    /// Gets the required scope value for tokens used with this permission.
    ///
    /// The scope "Scope.trelnex-auth-amazon-tests-authentication-1" is a fixed test value
    /// that identifies what access level the token grants. This is used in two ways:
    ///
    /// 1. When generating test tokens with TestJwtProvider, this value is included in the token's
    ///    scope claim for tokens that should have proper access to endpoints protected by this permission.
    ///
    /// 2. Authorization policies verify that tokens contain this scope value before granting access.
    ///
    /// The AuthenticationTests class validates scope checking by testing endpoints with tokens
    /// containing both correct and incorrect scope values.
    /// </summary>
    /// <param name="configuration">Configuration properties (not used in this implementation).</param>
    /// <returns>The fixed scope value "Scope.trelnex-auth-amazon-tests-authentication-1".</returns>
    public string GetScope(
        IConfiguration configuration)
    {
        return "Scope.trelnex-auth-amazon-tests-authentication-1";
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// A test role-based permission policy that enforces the "test.role.1" role requirement.
    ///
    /// This nested class implements IPermissionPolicy and defines the specific role requirement
    /// for endpoints protected by TestPermission1. In BaseApiTests.cs, multiple endpoints are
    /// protected with RequirePermission&lt;TestPermission1.TestRolePolicy&gt;, which enforces
    /// this policy.
    ///
    /// When a request is made to these endpoints:
    /// 1. The authentication system validates the token (signature, audience, issuer, etc.)
    /// 2. This policy verifies that the token contains the "test.role.1" role
    /// 3. Access is granted only if both validations pass
    ///
    /// AuthenticationTests verifies this policy by testing:
    /// - Tokens with the correct role (should succeed)
    /// - Tokens with incorrect roles (should be rejected)
    /// - Tokens with no roles (should be rejected)
    ///
    /// This TestRolePolicy is intentionally different from TestPermission2.TestRolePolicy to
    /// demonstrate separate access control for different endpoints.
    /// </summary>
    public class TestRolePolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the roles required for authorization under this policy.
        ///
        /// This property returns an array containing only "test.role.1", which means
        /// that any JWT token must contain this exact role in its "roles" claim to
        /// be granted access to endpoints protected by this policy.
        /// </summary>
        public string[] RequiredRoles => [ "test.role.1" ];
    }

    #endregion
}
