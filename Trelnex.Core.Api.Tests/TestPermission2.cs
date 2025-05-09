using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Api.Authentication;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test implementation of <see cref="IPermission"/> that configures the second JWT authentication
/// and authorization scheme for testing multi-scheme role-based access control.
///
/// This class complements TestPermission1 in the test authentication framework:
///
/// 1. It configures the second of two parallel authentication schemes in the test environment,
///    with its own distinct bearer scheme, audience, issuer, and role requirements.
///
/// 2. It defines authentication parameters for validating tokens in the second scheme,
///    using the same TestAlgorithm security key but with different audience and issuer values.
///
/// 3. It provides a nested TestRolePolicy class that specifies the "test.role.2" role requirement
///    used to protect the /testRolePolicy2 endpoint in BaseApiTests.
///
/// 4. It enables AuthenticationTests to verify proper multi-scheme authentication by testing endpoints
///    protected with RequirePermission&lt;TestPermission2.TestRolePolicy&gt;.
///
/// The class works together with TestPermission1 to demonstrate multiple authentication schemes
/// operating side-by-side. This pattern validates that each scheme correctly enforces its own
/// requirements independently, preventing tokens intended for one scheme from accessing endpoints
/// protected by the other scheme.
///
/// Endpoints using this permission require tokens with audience "Audience.trelnex-auth-amazon-tests-authentication-2",
/// issuer "Issuer.trelnex-auth-amazon-tests-authentication-2", and the "test.role.2" role.
/// </summary>
internal class TestPermission2 : IPermission
{
    #region Public Properties

    /// <summary>
    /// Gets the JWT bearer token scheme used for this permission's authentication.
    ///
    /// This unique scheme identifier ("Bearer.trelnex-auth-amazon-tests-authentication-2")
    /// distinguishes this authentication scheme from TestPermission1's scheme. In a multi-scheme
    /// environment, this separation ensures that each scheme maintains its own validation logic
    /// and protected resources.
    ///
    /// When the API framework receives a token, it uses this scheme identifier to determine
    /// which validation parameters to apply, ensuring that tokens intended for one scheme
    /// cannot access resources protected by another scheme.
    /// </summary>
    public string JwtBearerScheme => "Bearer.trelnex-auth-amazon-tests-authentication-2";

    #endregion

    #region Public Methods

    /// <summary>
    /// Configures JWT Bearer token authentication for this permission's scheme.
    ///
    /// This method sets up JWT Bearer authentication with specific token validation parameters
    /// for the "Bearer.trelnex-auth-amazon-tests-authentication-2" scheme. It configures:
    ///
    /// 1. Audience validation - requiring "Audience.trelnex-auth-amazon-tests-authentication-2"
    /// 2. Issuer validation - requiring "Issuer.trelnex-auth-amazon-tests-authentication-2"
    /// 3. Token signature validation - using TestAlgorithm's security key (same as Permission1)
    /// 4. Token lifetime validation - ensuring tokens aren't expired
    ///
    /// While this method's implementation is similar to TestPermission1.AddAuthentication,
    /// the key differences are the audience and issuer values. These differences allow
    /// AuthenticationTests to verify that tokens created for one scheme cannot be used
    /// with endpoints protected by the other scheme.
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
                        ValidIssuer = "Issuer.trelnex-auth-amazon-tests-authentication-2",

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
    /// requires the "test.role.2" role for access. In BaseApiTests.cs, the /testRolePolicy2 endpoint
    /// is protected using RequirePermission&lt;TestPermission2.TestRolePolicy&gt;, enforcing
    /// that only tokens containing the "test.role.2" role can access this endpoint.
    ///
    /// This creates a clear separation between endpoints protected by TestPermission1 (which require
    /// the "test.role.1" role) and those protected by TestPermission2, allowing AuthenticationTests
    /// to verify that each policy correctly enforces its specific role requirements.
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
    /// The audience "Audience.trelnex-auth-amazon-tests-authentication-2" distinguishes tokens
    /// intended for this authentication scheme from those intended for TestPermission1.
    /// This distinct audience value is used in two important ways:
    ///
    /// 1. When generating test tokens with TestJwtProvider, this value must be included as the audience
    ///    claim in tokens that should be valid for the /testRolePolicy2 endpoint.
    ///
    /// 2. During token validation, the authentication system verifies that the token's audience
    ///    claim matches this value before granting access to TestPermission2-protected endpoints.
    ///
    /// This distinct audience value allows AuthenticationTests to verify that tokens created for
    /// TestPermission1 (with a different audience) cannot access TestPermission2-protected endpoints.
    /// </summary>
    /// <param name="configuration">Configuration properties (not used in this implementation).</param>
    /// <returns>The fixed audience value "Audience.trelnex-auth-amazon-tests-authentication-2".</returns>
    public string GetAudience(
        IConfiguration configuration)
    {
        return "Audience.trelnex-auth-amazon-tests-authentication-2";
    }

    /// <summary>
    /// Gets the required scope value for tokens used with this permission.
    ///
    /// The scope "Scope.trelnex-auth-amazon-tests-authentication-2" is distinct from the scope
    /// used by TestPermission1. This separate scope value is used in the following ways:
    ///
    /// 1. When generating test tokens with TestJwtProvider, this value must be included in the token's
    ///    scope claim for tokens that should have proper access to /testRolePolicy2.
    ///
    /// 2. Authorization policies verify that tokens contain this scope value when accessing
    ///    endpoints protected by TestPermission2.
    ///
    /// The AuthenticationTests class validates proper scope checking by testing the /testRolePolicy2
    /// endpoint with tokens containing both correct and incorrect scope values.
    /// </summary>
    /// <param name="configuration">Configuration properties (not used in this implementation).</param>
    /// <returns>The fixed scope value "Scope.trelnex-auth-amazon-tests-authentication-2".</returns>
    public string GetScope(
        IConfiguration configuration)
    {
        return "Scope.trelnex-auth-amazon-tests-authentication-2";
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// A test role-based permission policy that enforces the "test.role.2" role requirement.
    ///
    /// This nested class implements IPermissionPolicy and defines the specific role requirement
    /// for endpoints protected by TestPermission2. In BaseApiTests.cs, the /testRolePolicy2 endpoint
    /// is protected with RequirePermission&lt;TestPermission2.TestRolePolicy&gt;, which enforces
    /// this policy.
    ///
    /// The key distinction between this policy and TestPermission1.TestRolePolicy is the required role.
    /// This policy requires "test.role.2" while TestPermission1.TestRolePolicy requires "test.role.1".
    /// This difference allows the test framework to verify that:
    ///
    /// 1. Each policy correctly enforces its own role requirements
    /// 2. A token with "test.role.1" cannot access endpoints protected by this policy
    /// 3. A token with "test.role.2" cannot access endpoints protected by TestPermission1.TestRolePolicy
    ///
    /// AuthenticationTests uses this separation to validate proper role-based access control
    /// across multiple authentication schemes.
    /// </summary>
    public class TestRolePolicy : IPermissionPolicy
    {
        /// <summary>
        /// Gets the roles required for authorization under this policy.
        ///
        /// This property returns an array containing only "test.role.2", which means
        /// that any JWT token must contain this exact role in its "roles" claim to
        /// be granted access to endpoints protected by this policy.
        ///
        /// The difference between this value and TestPermission1.TestRolePolicy.RequiredRoles
        /// is intentional and central to testing proper role-based authorization.
        /// </summary>
        public string[] RequiredRoles => ["test.role.2"];
    }

    #endregion
}
