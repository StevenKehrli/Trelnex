using JWT.Algorithms;
using JWT.Builder;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test utility class for generating JWT tokens with configurable claims for authentication testing.
///
/// TestJwtProvider is a cornerstone of the test authentication framework, enabling tests to create
/// tokens with precise combinations of claims to validate various authentication and authorization scenarios.
///
/// Key features and usage:
///
/// 1. Token Generation - Creates fully-formed JWT tokens with configurable audience, issuer,
///    principal ID, scopes, and roles, allowing tests to precisely control token content.
///
/// 2. Multiple Authentication Schemes - The BaseApiTests class creates two instances of this provider,
///    _jwtProvider1 and _jwtProvider2, each configured with different issuers to test multiple
///    authentication schemes (TestPermission1 and TestPermission2).
///
/// 3. Authorization Testing - AuthenticationTests uses this class to generate tokens with various
///    combinations of scopes and roles to verify that endpoints correctly enforce authorization rules.
///
/// 4. Claims Flexibility - Allows direct control over audience, principalId, scopes and roles,
///    enabling tests to verify token validation for both valid and invalid scenarios.
///
/// This provider works in conjunction with TestAlgorithm to sign tokens consistently, allowing
/// TestPermission1 and TestPermission2 to validate the signatures during authentication.
/// The tests can then verify that endpoints protected by different permissions correctly
/// validate tokens and enforce their specific audience, issuer, scope, and role requirements.
/// </summary>
internal class TestJwtProvider
{
    #region Private Fields

    /// <summary>
    /// The expiration time of the token in minutes.
    ///
    /// This value is used to set the "exp" (expiration time) claim in generated tokens.
    /// The authentication system verifies that this time has not passed during token validation.
    /// Tests can use this to verify token lifetime validation by creating expired tokens
    /// (though the constructor enforces a minimum expiration time to avoid clock skew issues).
    /// </summary>
    private readonly int _expirationInMinutes;

    /// <summary>
    /// The issuer of the token, set during construction.
    ///
    /// The issuer identifies which authentication authority generated the token.
    /// In BaseApiTests, two TestJwtProvider instances are created with different issuers:
    /// - _jwtProvider1 uses "Issuer.trelnex-auth-amazon-tests-authentication-1"
    /// - _jwtProvider2 uses "Issuer.trelnex-auth-amazon-tests-authentication-2"
    ///
    /// Each TestPermission class expects tokens from the corresponding issuer, allowing
    /// the test framework to verify that issuer validation works correctly.
    /// </summary>
    private readonly string _issuer;

    /// <summary>
    /// The JWT algorithm used to sign the token.
    ///
    /// This algorithm (TestAlgorithm) ensures consistent token signing and verification
    /// across the test framework. TestAlgorithm provides both the signing operation for
    /// this class and the security key used by TestPermission classes during validation.
    /// </summary>
    private readonly IJwtAlgorithm _jwtAlgorithm;

    /// <summary>
    /// The key ID of the signing key.
    ///
    /// This value is included in the token header to identify which key was used to sign the token.
    /// In a production environment, this enables key rotation. In the test environment, it's set to
    /// a consistent value ("KeyId.trelnex-auth-amazon-tests-authentication") to match the key ID
    /// in TestAlgorithm.
    /// </summary>
    private readonly string _keyId;

    /// <summary>
    /// The refresh time of the token in minutes.
    ///
    /// This value is set to 5 minutes before expiration and is used to populate the RefreshOn
    /// property in the returned AccessToken. In a production environment, this would indicate
    /// when a client should proactively refresh a token to avoid expiration.
    /// </summary>
    private readonly int _refreshInMinutes;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TestJwtProvider"/> class with specific JWT parameters.
    ///
    /// This constructor configures a token provider for a specific authentication scheme. In BaseApiTests,
    /// two instances are created:
    ///
    /// 1. _jwtProvider1 - Uses issuer "Issuer.trelnex-auth-amazon-tests-authentication-1" to generate
    ///    tokens for endpoints protected by TestPermission1
    ///
    /// 2. _jwtProvider2 - Uses issuer "Issuer.trelnex-auth-amazon-tests-authentication-2" to generate
    ///    tokens for endpoints protected by TestPermission2
    ///
    /// Both use the same TestAlgorithm instance and key ID, as the signature validation is the same
    /// across both authentication schemes, but they differ in the issuer value.
    /// </summary>
    /// <param name="jwtAlgorithm">The JWT algorithm used to sign tokens, typically a TestAlgorithm instance.</param>
    /// <param name="keyId">The key ID included in the token header that identifies the signing key.</param>
    /// <param name="issuer">The issuer claim value that identifies the token's issuing authority.</param>
    /// <param name="expirationInMinutes">The token's validity period in minutes (minimum 15 minutes).</param>
    public TestJwtProvider(
        IJwtAlgorithm jwtAlgorithm,
        string keyId,
        string issuer,
        int expirationInMinutes)
    {
        _jwtAlgorithm = jwtAlgorithm;
        _keyId = keyId;
        _issuer = issuer;

        // Ensure expiration is a minimum of 15 minutes to avoid issues with clock skew.
        // Refresh is set to 5 minutes before expiration to allow for token refresh before expiration.
        _expirationInMinutes = Math.Max(15, expirationInMinutes);
        _refreshInMinutes = _expirationInMinutes - 5;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a JWT token with specified claims for testing authentication and authorization scenarios.
    ///
    /// This is the central method for generating test tokens with precise combinations of claims.
    /// AuthenticationTests uses this method extensively to create tokens with various claim configurations
    /// to verify that endpoints correctly validate tokens and enforce authorization requirements.
    ///
    /// Key aspects of token generation:
    ///
    /// 1. Standard Claims - The method sets standard JWT claims including issuer, audience, issued-at,
    ///    not-before, expiration, subject, and object ID.
    ///
    /// 2. Custom Claims - The method adds "scp" (scopes) and "roles" claims based on the parameters,
    ///    allowing tests to verify scope and role-based authorization.
    ///
    /// 3. Token Structure - The generated token follows standard JWT format with three parts:
    ///    - Header: Contains algorithm and key ID
    ///    - Payload: Contains all claims (issuer, audience, expiration, roles, etc.)
    ///    - Signature: Created using the provided algorithm (TestAlgorithm)
    ///
    /// 4. AccessToken Return - Returns a complete AccessToken object with the JWT string,
    ///    expiration time, refresh time, and token type.
    ///
    /// Test examples using this method:
    /// - Testing with missing scopes/roles to verify 403 Forbidden responses
    /// - Testing with incorrect audience to verify 401 Unauthorized responses
    /// - Testing with correct claims to verify successful authentication
    /// </summary>
    /// <param name="audience">The audience claim value (e.g., "Audience.trelnex-auth-amazon-tests-authentication-1")
    /// which must match the expected audience in the corresponding TestPermission.</param>
    /// <param name="principalId">The user identifier, included as both "sub" and "oid" claims.</param>
    /// <param name="scopes">Array of scope strings that will be joined and included as the "scp" claim.</param>
    /// <param name="roles">Array of role strings included as the "roles" claim for role-based authorization.</param>
    /// <returns>An AccessToken containing the JWT string, expiration, refresh time, and token type.</returns>
    public AccessToken Encode(
        string audience,
        string principalId,
        string[] scopes,
        string[] roles)
    {
        // Create the JWT builder.
        var jwtBuilder = JwtBuilder
            .Create()
            .WithAlgorithm(_jwtAlgorithm)
            .AddHeader(HeaderName.KeyId, _keyId); // Add the key ID to the header for key rotation.

        // Set the issuer.
        jwtBuilder.Issuer(_issuer);

        // Get the current date time.
        var dateTime = DateTime.UtcNow;
        var expiresOn = dateTime.AddMinutes(_expirationInMinutes);
        var refreshOn = dateTime.AddMinutes(_refreshInMinutes);

        // Set the issued at, not before, and expiration time.
        jwtBuilder
            .IssuedAt(dateTime)
            .NotBefore(dateTime)
            .ExpirationTime(expiresOn);

        // Set the audience.
        jwtBuilder.Audience(audience);

        // Add any scopes.
        if (scopes.Length > 0)
        {
            var scp = string.Join(" ", scopes);
            jwtBuilder.AddClaim("scp", scp); // "scp" is the standard claim name for scopes.
        }

        // Add any roles.
        if (roles.Length > 0)
        {
            jwtBuilder.AddClaim("roles", roles); // "roles" is a custom claim name for roles.
        }

        // Add the principalId as the oid and sub claims.
        jwtBuilder
            .AddClaim("oid", principalId) // "oid" is the standard claim name for object ID.
            .AddClaim("sub", principalId); // "sub" is the standard claim name for subject.

        // Encode the JWT token.
        var token = jwtBuilder.Encode();

        return new AccessToken()
        {
            Token = token,
            ExpiresOn = expiresOn,
            RefreshOn = refreshOn,
            TokenType = "Bearer"
        };
    }

    #endregion
}
