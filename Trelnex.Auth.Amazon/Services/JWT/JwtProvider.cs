using JWT.Algorithms;
using JWT.Builder;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

public interface IJwtProvider
{
    /// <summary>
    /// Encodes a JWT token for the specified caller identity.
    /// </summary>
    /// <param name="audience">The audience of the token.</param>
    /// <param name="principalId">The ARN of the caller.</param>
    /// <param name="scopes">The scopes of the token.</param>
    /// <param name="roles">The roles assigned to the caller identity to be encoded as the roles claim.</param>
    /// <returns>The JWT token.</returns>
    AccessToken Encode(
        string audience,
        string principalId,
        string[] scopes,
        string[] roles);
}

internal class JwtProvider : IJwtProvider
{
    private readonly IJwtAlgorithm _jwtAlgorithm;

    private readonly string _keyId;

    private readonly string _issuer;

    private readonly int _expirationInMinutes;

    private readonly int _refreshInMinutes;

    private JwtProvider(
        IJwtAlgorithm jwtAlgorithm,
        string keyId,
        string issuer,
        int expirationInMinutes)
    {
        _jwtAlgorithm = jwtAlgorithm;
        _keyId = keyId;
        _issuer = issuer;

        // expiration is a minimum of 15 minutes
        // refresh is 5 minutes before expiration
        _expirationInMinutes = Math.Max(15, expirationInMinutes);
        _refreshInMinutes = _expirationInMinutes - 5;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="JwtProvider"/>.
    /// </summary>
    /// <param name="jwtAlgorithm">The jwt algorithm to use for signing the token.</param>
    /// <param name="keyId">The key id to use for signing the token.</param>
    /// <param name="issuer">The issuer of the token.</param>
    /// <param name="expirationInMinutes">The expiration time of the jwt token in minutes.</param>
    /// <returns>The <see cref="JwtProvider"/>.</returns>
    public static IJwtProvider Create(
        IJwtAlgorithm jwtAlgorithm,
        string keyId,
        string issuer,
        int expirationInMinutes)
    {
        // create the jwt provider
        return new JwtProvider(
            jwtAlgorithm,
            keyId,
            issuer,
            expirationInMinutes);
    }

    /// <summary>
    /// Encodes a jwt token for the specified caller identity.
    /// </summary>
    /// <param name="audience">The audience of the token.</param>
    /// <param name="principalId">The ARN of the caller.</param>
    /// <param name="scopes">The scopes of the token.</param>
    /// <param name="roles">The roles assigned to the caller identity to be encoded as the roles claim.</param>
    /// <returns>The jwt token.</returns>
    public AccessToken Encode(
        string audience,
        string principalId,
        string[] scopes,
        string[] roles)
    {
        // create the jwt builder
        var jwtBuilder = JwtBuilder
            .Create()
            .WithAlgorithm(_jwtAlgorithm)
            .AddHeader(HeaderName.KeyId, _keyId);

        // set the issuer
        jwtBuilder.Issuer(_issuer);

        // get the current date time
        var dateTime = DateTime.UtcNow;
        var expiresOn = dateTime.AddMinutes(_expirationInMinutes);
        var refreshOn = dateTime.AddMinutes(_refreshInMinutes);

        // set the issued at, not bofore, and expiration time
        jwtBuilder
            .IssuedAt(dateTime)
            .NotBefore(dateTime)
            .ExpirationTime(expiresOn);

        // set the audience
        jwtBuilder.Audience(audience);

        // add any scopes
        if (scopes.Length > 0)
        {
            var scp = string.Join(" ", scopes);
            jwtBuilder.AddClaim("scp", scp);
        }

        // add any roles
        if (roles.Length > 0)
        {
            jwtBuilder.AddClaim("roles", roles);
        }

        // add the principalId as the oid and sub claims
        jwtBuilder
            .AddClaim("oid", principalId)
            .AddClaim("sub", principalId);

        // encode the jwt token
        var token = jwtBuilder.Encode();

        return new AccessToken()
        {
            Token = token,
            ExpiresOn = expiresOn,
            RefreshOn = refreshOn,
            TokenType = "Bearer"
        };
    }
}
