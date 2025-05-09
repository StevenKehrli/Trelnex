using JWT.Algorithms;
using JWT.Builder;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test utility class for generating JWT tokens with specific claims, issuer, and algorithm for testing authentication and authorization scenarios.
/// It allows tests to simulate different user roles, scopes, and audiences without relying on an external authentication server.
/// </summary>
internal class TestJwtProvider
{
    #region Private Fields

    /// <summary>
    /// The expiration time of the token in minutes.
    /// </summary>
    private readonly int _expirationInMinutes;

    /// <summary>
    /// The issuer of the token.
    /// </summary>
    private readonly string _issuer;

    /// <summary>
    /// The JWT algorithm used to sign the token.
    /// </summary>
    private readonly IJwtAlgorithm _jwtAlgorithm;

    /// <summary>
    /// The key ID of the signing key.
    /// </summary>
    private readonly string _keyId;

    /// <summary>
    /// The refresh time of the token in minutes.
    /// </summary>
    private readonly int _refreshInMinutes;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TestJwtProvider"/> class.
    /// </summary>
    /// <param name="jwtAlgorithm">The JWT algorithm used to sign the token.</param>
    /// <param name="keyId">The key ID of the signing key.</param>
    /// <param name="issuer">The issuer of the token.</param>
    /// <param name="expirationInMinutes">The expiration time of the token in minutes.</param>
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
    /// Encodes a JWT token for the specified caller identity.
    /// </summary>
    /// <param name="audience">The audience of the token.</param>
    /// <param name="principalId">The principal ID of the caller.</param>
    /// <param name="scopes">The scopes of the token.</param>
    /// <param name="roles">The roles assigned to the caller identity to be encoded as the roles claim.</param>
    /// <returns>The JWT token.</returns>
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
