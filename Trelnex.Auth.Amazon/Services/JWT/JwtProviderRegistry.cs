using System.Configuration;
using Amazon;
using Amazon.Runtime;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

public interface IJwtProviderRegistry
{
    /// <summary>
    /// Get the open id configuration
    /// </summary>
    OpenIdConfiguration OpenIdConfiguration { get; }

    /// <summary>
    /// Get the json web key set
    /// </summary>
    JsonWebKeySet JWKS { get; }

    /// <summary>
    /// Get the default JWT provider.
    /// </summary>
    /// <returns>The JWT provider.</returns>
    IJwtProvider GetProvider();

    /// <summary>
    /// Get the JWT provider with the specified attribute.
    /// </summary>
    /// <param name="attribute">The specified attribute on the provider.</param>
    /// <returns>The JWT provider.</returns>
    IJwtProvider GetProvider<T>(
        T attribute);
}

internal class JwtProviderRegistry : IJwtProviderRegistry
{
    private readonly OpenIdConfiguration _openIdConfiguration;

    private readonly JsonWebKeySet _jwks;

    private readonly IJwtProvider _defaultJwtProvider;

    private readonly Dictionary<RegionEndpoint, IJwtProvider> _regionalJwtProviders;

    private JwtProviderRegistry(
        OpenIdConfiguration openIdConfiguration,
        JsonWebKeySet jwks,
        IJwtProvider defaultJwtProvider,
        (RegionEndpoint region, IJwtProvider jwtProvider)[]? regionalJwtProviders)
    {
        _openIdConfiguration = openIdConfiguration;
        _jwks = jwks;

        // set the default provider
        _defaultJwtProvider = defaultJwtProvider;

        // set the regional providers
        _regionalJwtProviders = regionalJwtProviders?
            .ToDictionary(
                x => x.region,
                x => x.jwtProvider) ?? [];
    }

    /// <summary>
    /// Get the open id configuration
    /// </summary>
    public OpenIdConfiguration OpenIdConfiguration => _openIdConfiguration;

    /// <summary>
    /// Get the json web key set
    /// </summary>
    public JsonWebKeySet JWKS => _jwks;

    /// <summary>
    /// Creates a new instance of the <see cref="KMSAlgorithmProvider"/>.
    /// </summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="bootstrapLogger">The <see cref="ILogger"/> to write the KMSAlgorithmProvider bootstrap logs.</param>
    /// <param name="credentialProvider">The credential provider to get the AWS credentials.</param>
    /// <returns>The <see cref="IJwtProviderRegistry"/>.</returns>
    public static IJwtProviderRegistry Create(
        IConfiguration configuration,
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        var jwtConfiguration = configuration
            .GetSection("JWT")
            .Get<JwtConfiguration>()
            ?? throw new ConfigurationErrorsException("The JWT configuration is not found.");

        var openIdConfiguration = configuration
            .GetSection("JWT:openid-configuration")
            .Get<OpenIdConfiguration>()
            ?? throw new ConfigurationErrorsException("The openid-configuration configuration is not found.");

        // create the collection of kms algorithms
        var algorithmCollection = KMSAlgorithmCollection.Create(
            bootstrapLogger,
            credentialProvider,
            jwtConfiguration.DefaultKey,
            jwtConfiguration.RegionalKeys,
            jwtConfiguration.SecondaryKeys);

        // create the json web key set
        var jwks = new JsonWebKeySet();

        // add the default jwk to the set
        jwks.Keys.Add(algorithmCollection.DefaultAlgorithm.JWK);

        // add the regional jwks to the set
        Array.ForEach(algorithmCollection.RegionalAlgorithms ?? [], algorithm =>
        {
            jwks.Keys.Add(algorithm.JWK);
        });

        // add the secondary jwks to the set
        Array.ForEach(algorithmCollection.SecondaryAlgorithms ?? [], algorithm =>
        {
            jwks.Keys.Add(algorithm.JWK);
        });

        // get the default jwt provider
        var defaultJwtProvider = new JwtProvider(
            algorithmCollection.DefaultAlgorithm,
            algorithmCollection.DefaultAlgorithm.JWK.KeyId,
            openIdConfiguration.Issuer,
            jwtConfiguration.ExpirationInMinutes);

        // get the regional providers
        var regionalJwtProviders = algorithmCollection.RegionalAlgorithms?
            .Select(algorithm => (
                region: algorithm.Region,
                jwtProvider: new JwtProvider(
                    algorithm,
                    algorithm.JWK.KeyId,
                    openIdConfiguration.Issuer,
                    jwtConfiguration.ExpirationInMinutes) as IJwtProvider))
            .ToArray();

        // create the algorithm provider
        return new JwtProviderRegistry(
            openIdConfiguration,
            jwks,
            defaultJwtProvider,
            regionalJwtProviders);
    }

    /// <summary>
    /// Get the default JWT provider.
    /// </summary>
    /// <returns>The JWT provider.</returns>
    public IJwtProvider GetProvider()
    {
        return _defaultJwtProvider;
    }

    /// <summary>
    /// Get the JWT provider with the specified attribute.
    /// </summary>
    /// <param name="attribute">The specified attribute on the provider.</param>
    /// <returns>The JWT provider.</returns>
    public IJwtProvider GetProvider<T>(
        T attribute)
    {
        // cast the attribute to a RegionEndpoint
        var region = attribute as RegionEndpoint
            ?? throw new ArgumentException($"The attribute '{attribute}' is not a valid '{nameof(RegionEndpoint)}'.");

        // get the jwt provider
        return _regionalJwtProviders.TryGetValue(region, out var jwtProvider)
            ? jwtProvider
            : _defaultJwtProvider;
    }
}
