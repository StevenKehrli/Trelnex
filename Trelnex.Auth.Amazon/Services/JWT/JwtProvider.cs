using System.Configuration;
using Amazon;
using Amazon.Runtime;
using JWT.Builder;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

internal interface IJwtProvider
{
    /// <summary>
    /// Get the json web key set
    /// </summary>
    JsonWebKeySet JWKS { get; }

    /// <summary>
    /// Encodes a JWT token for the specified caller identity.
    /// </summary>
    /// <param name="region">The region of the caller identity.</param>
    /// <param name="principalId">The ARN of the caller.</param>
    /// <param name="audience">The audience of the token.</param>
    /// <param name="scopes">The scopes of the token.</param>
    /// <param name="roles">The roles assigned to the caller identity to be encoded as the roles claim.</param>
    /// <returns>The JWT token.</returns>
    AccessToken CreateToken(
        string region,
        string principalId,
        string audience,
        string[] scopes,
        string[] roles);

    /// <summary>
    /// Get the open id configuration
    /// </summary>
    /// <returns>The open id configuration</returns>
    OpenIdConfiguration GetOpenIdConfiguration();
}

internal class JwtProvider : IJwtProvider
{
    private readonly KMSAlgorithmProvider _algorithmProvider;
    private readonly JwtConfiguration _jwtConfiguration;

    private JwtProvider(
        KMSAlgorithmProvider algorithmProvider,
        JwtConfiguration jwtConfiguration)
    {
        _algorithmProvider = algorithmProvider;
        _jwtConfiguration = jwtConfiguration;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="JwtProvider"/>.
    /// </summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the bootstrap logs.</param>
    /// <param name="credentialProvider">The credential provider to get the AWS credentials.</param>
    /// <returns>The <see cref="JwtProvider"/>.</returns>
    public static JwtProvider Create(
        IConfiguration configuration,
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        var jwtConfiguration = configuration
            .GetSection("JWT")
            .Get<JwtConfiguration>()
            ?? throw new ConfigurationErrorsException("The JWT configuration is not found.");

        // create the kms algorithm provider
        var algorithmProvider = KMSAlgorithmProvider.Create(
            bootstrapLogger,
            credentialProvider,
            jwtConfiguration.KMSAlgorithms);

        // create the jwt factory
        return new JwtProvider(
            algorithmProvider,
            jwtConfiguration);
    }

    /// <summary>
    /// Get the json web key set
    /// </summary>
    public JsonWebKeySet JWKS => _algorithmProvider.JWKS;

    /// <summary>
    /// Encodes a JWT token for the specified caller identity.
    /// </summary>
    /// <param name="region">The region of the caller identity.</param>
    /// <param name="principalId">The ARN of the caller.</param>
    /// <param name="audience">The audience of the token.</param>
    /// <param name="scopes">The scopes of the token.</param>
    /// <param name="roles">The roles assigned to the caller identity to be encoded as the roles claim.</param>
    /// <returns>The JWT token.</returns>
    public AccessToken CreateToken(
        string region,
        string principalId,
        string audience,
        string[] scopes,
        string[] roles)
    {
        // get the algorithm
        var regionEndpoint = RegionEndpoint.GetBySystemName(region);
        var algorithm = _algorithmProvider.GetAlgorithm(regionEndpoint);

        // create the jwt builder with the specified algorithm
        var jwtBuilder = JwtBuilder
            .Create()
            .WithAlgorithm(algorithm)
            .AddHeader(HeaderName.KeyId, algorithm.JWK.KeyId);

        // set the issuer
        jwtBuilder
            .Issuer(_jwtConfiguration.OpenIdConfiguration.Issuer);

        // expiration is a minimum of 15 minutes
        // refresh is 5 minutes before expiration
        var expirationInMinutes = Math.Max(15, _jwtConfiguration.ExpirationInMinutes);
        var refreshInMinutes = expirationInMinutes - 5;

        // get the current date time
        var dateTime = DateTime.UtcNow;
        var expiresOn = dateTime.AddMinutes(expirationInMinutes);
        var refreshOn = dateTime.AddMinutes(refreshInMinutes);

        // set the issued at, not bofore, and expiration time
        jwtBuilder
            .IssuedAt(dateTime)
            .NotBefore(dateTime)
            .ExpirationTime(expiresOn);

        // set the audience and scope claims
        var scp = string.Join(" ", scopes);
        jwtBuilder
            .Audience(audience)
            .AddClaim("scp", scp);

        // add the roles claims
        jwtBuilder
            .AddClaim("roles", roles);

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

    /// <summary>
    /// Get the open id configuration
    /// </summary>
    /// <returns>The open id configuration</returns>
    public OpenIdConfiguration GetOpenIdConfiguration()
    {
        return _jwtConfiguration.OpenIdConfiguration;
    }
}
