using System.Configuration;
using Amazon;
using Amazon.Runtime;
using Microsoft.IdentityModel.Tokens;
using Trelnex.Core.Identity;

namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Defines operations for retrieving JWT providers and related configurations.
/// </summary>
/// <remarks>
/// This interface provides access to JWT providers for token generation, OpenID Connect
/// configuration, and JSON Web Key Set (JWKS) for token validation. It manages
/// both default and region-specific JWT providers, allowing for region-aware
/// token issuance to improve performance and availability.
///
/// The registry serves as a central access point for JWT-related services, enabling:
/// - Discovery of OpenID Connect endpoints via the OpenID Configuration
/// - Token validation via the JSON Web Key Set
/// - Region-aware token issuance for improved performance
/// - Consistent token generation across different parts of the application
/// </remarks>
public interface IJwtProviderRegistry
{
    /// <summary>
    /// Gets the OpenID Connect configuration.
    /// </summary>
    /// <remarks>
    /// The OpenID Configuration provides standard metadata about the token service,
    /// including issuer URI, supported algorithms, endpoint locations, and other
    /// discovery information defined by the OpenID Connect Discovery specification.
    ///
    /// This configuration is published at the /.well-known/openid-configuration
    /// endpoint and enables OpenID Connect clients to dynamically configure
    /// themselves to interact with the token service.
    /// </remarks>
    OpenIdConfiguration OpenIdConfiguration { get; }

    /// <summary>
    /// Gets the JSON Web Key Set (JWKS).
    /// </summary>
    /// <remarks>
    /// The JWKS contains the public keys used to validate JWT signatures.
    /// It includes all active signing keys (default, regional, and secondary),
    /// allowing clients to verify tokens regardless of which key was used to sign them.
    ///
    /// This key set is published at the /.well-known/jwks.json endpoint in accordance
    /// with the OpenID Connect and OAuth 2.0 standards, enabling dynamic key rotation
    /// without client reconfiguration.
    /// </remarks>
    JsonWebKeySet JWKS { get; }

    /// <summary>
    /// Gets the default JWT provider.
    /// </summary>
    /// <returns>The default JWT provider.</returns>
    /// <remarks>
    /// The default provider uses the primary signing key configured in the system.
    /// It is used when no specific region is required or when a regional provider
    /// is not available for the requested region.
    /// </remarks>
    IJwtProvider GetProvider();

    /// <summary>
    /// Gets a JWT provider associated with the specified attribute.
    /// </summary>
    /// <typeparam name="T">The type of the attribute.</typeparam>
    /// <param name="attribute">The attribute identifying a specific provider (e.g., AWS Region).</param>
    /// <returns>The region-specific JWT provider if available; otherwise, the default provider.</returns>
    /// <remarks>
    /// This method supports retrieving region-specific JWT providers, which can improve
    /// performance by using signing keys in the same AWS region as the caller.
    /// Currently, the only supported attribute type is <see cref="RegionEndpoint"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the attribute cannot be cast to a RegionEndpoint.
    /// </exception>
    IJwtProvider GetProvider<T>(
        T attribute);
}

/// <summary>
/// Manages JWT providers and related configurations for JWT token generation and validation.
/// </summary>
/// <remarks>
/// This registry serves as a central repository for JWT-related services. It maintains:
/// - OpenID Connect configuration for discovery
/// - JSON Web Key Set (JWKS) for token validation
/// - Default JWT provider for general token issuance
/// - Region-specific JWT providers for optimized performance
///
/// The registry creates and initializes JWT providers with appropriate AWS KMS signing
/// algorithms based on the application configuration, supporting key rotation and
/// regional deployment strategies.
/// </remarks>
internal class JwtProviderRegistry : IJwtProviderRegistry
{
    #region Private Fields

    /// <summary>
    /// The OpenID Connect configuration for service discovery.
    /// </summary>
    private readonly OpenIdConfiguration _openIdConfiguration;

    /// <summary>
    /// The JSON Web Key Set containing public keys for validating tokens.
    /// </summary>
    private readonly JsonWebKeySet _jwks;

    /// <summary>
    /// The default JWT provider used when no specific region is requested.
    /// </summary>
    private readonly IJwtProvider _defaultJwtProvider;

    /// <summary>
    /// Dictionary of region-specific JWT providers keyed by AWS region.
    /// </summary>
    private readonly Dictionary<RegionEndpoint, IJwtProvider> _regionalJwtProviders;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtProviderRegistry"/> class.
    /// </summary>
    /// <param name="openIdConfiguration">The OpenID Connect configuration.</param>
    /// <param name="jwks">The JSON Web Key Set for token validation.</param>
    /// <param name="defaultJwtProvider">The default JWT provider.</param>
    /// <param name="regionalJwtProviders">Optional array of region-specific JWT providers.</param>
    /// <remarks>
    /// This constructor is private to enforce creation through the factory method,
    /// ensuring proper initialization of all dependencies.
    /// </remarks>
    private JwtProviderRegistry(
        OpenIdConfiguration openIdConfiguration,
        JsonWebKeySet jwks,
        IJwtProvider defaultJwtProvider,
        (RegionEndpoint regionEndpoint, IJwtProvider jwtProvider)[]? regionalJwtProviders)
    {
        // Set the OpenID Connect configuration.
        _openIdConfiguration = openIdConfiguration;

        // Set the JSON Web Key Set.
        _jwks = jwks;

        // Set the default provider.
        _defaultJwtProvider = defaultJwtProvider;

        // Set the regional providers.
        _regionalJwtProviders = regionalJwtProviders?
            .ToDictionary(
                x => x.regionEndpoint,
                x => x.jwtProvider) ?? [];
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the OpenID Connect configuration.
    /// </summary>
    /// <remarks>
    /// This configuration is published at the /.well-known/openid-configuration endpoint
    /// to enable OpenID Connect clients to discover service endpoints and capabilities.
    /// </remarks>
    public OpenIdConfiguration OpenIdConfiguration => _openIdConfiguration;

    /// <summary>
    /// Gets the JSON Web Key Set (JWKS).
    /// </summary>
    /// <remarks>
    /// This key set is published at the /.well-known/jwks.json endpoint to enable
    /// clients to validate JWT signatures. It includes all active signing keys.
    /// </remarks>
    public JsonWebKeySet JWKS => _jwks;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="JwtProviderRegistry"/>.
    /// </summary>
    /// <param name="configuration">The application configuration containing JWT settings.</param>
    /// <param name="bootstrapLogger">Logger for capturing initialization events.</param>
    /// <param name="credentialProvider">Provider for AWS credentials used with KMS.</param>
    /// <returns>A configured JWT provider registry.</returns>
    /// <remarks>
    /// This factory method performs the following operations:
    /// 1. Loads JWT and OpenID Connect configuration from application settings
    /// 2. Creates a collection of KMS algorithms for different regions and key rotation scenarios
    /// 3. Constructs a JSON Web Key Set with public keys from all configured algorithms
    /// 4. Creates default and region-specific JWT providers using the appropriate algorithms
    /// 5. Assembles all components into a JwtProviderRegistry
    ///
    /// The created registry supports token issuance optimized for different AWS regions
    /// and maintains a complete set of public keys for token validation.
    /// </remarks>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when required JWT configuration sections are missing.
    /// </exception>
    public static IJwtProviderRegistry Create(
        IConfiguration configuration,
        ILogger bootstrapLogger,
        ICredentialProvider<AWSCredentials> credentialProvider)
    {
        // Load JWT configuration from application settings.
        var jwtConfiguration = configuration
            .GetSection("JWT")
            .Get<JwtConfiguration>()
            ?? throw new ConfigurationErrorsException("The JWT configuration is not found.");

        // Load OpenID Connect configuration from application settings.
        var openIdConfiguration = configuration
            .GetSection("JWT:openid-configuration")
            .Get<OpenIdConfiguration>()
            ?? throw new ConfigurationErrorsException("The openid-configuration configuration is not found.");

        // Create the collection of KMS algorithms.
        var algorithmCollection = KMSAlgorithmCollection.Create(
            bootstrapLogger,
            credentialProvider,
            jwtConfiguration.DefaultKey,
            jwtConfiguration.RegionalKeys,
            jwtConfiguration.SecondaryKeys);

        // Create the JSON Web Key Set.
        var jwks = new JsonWebKeySet();

        // Add the default jwk to the set.
        jwks.Keys.Add(algorithmCollection.DefaultAlgorithm.JWK);

        // Add the regional jwks to the set.
        Array.ForEach(algorithmCollection.RegionalAlgorithms ?? [], algorithm =>
        {
            jwks.Keys.Add(algorithm.JWK);
        });

        // Add the secondary jwks to the set.
        Array.ForEach(algorithmCollection.SecondaryAlgorithms ?? [], algorithm =>
        {
            jwks.Keys.Add(algorithm.JWK);
        });

        // Get the default JWT provider.
        var defaultJwtProvider = new JwtProvider(
            algorithmCollection.DefaultAlgorithm,
            algorithmCollection.DefaultAlgorithm.JWK.KeyId,
            openIdConfiguration.Issuer,
            jwtConfiguration.ExpirationInMinutes);

        // Get the regional providers.
        var regionalJwtProviders = algorithmCollection.RegionalAlgorithms?
            .Select(algorithm => (
                regionEndpoint: algorithm.RegionEndpoint,
                jwtProvider: new JwtProvider(
                    algorithm,
                    algorithm.JWK.KeyId,
                    openIdConfiguration.Issuer,
                    jwtConfiguration.ExpirationInMinutes) as IJwtProvider))
            .ToArray();

        // Create the algorithm provider.
        return new JwtProviderRegistry(
            openIdConfiguration,
            jwks,
            defaultJwtProvider,
            regionalJwtProviders);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the default JWT provider.
    /// </summary>
    /// <returns>The default JWT provider configured for the system.</returns>
    /// <remarks>
    /// This method returns the provider that uses the primary signing key.
    /// It is used when no specific region is required.
    /// </remarks>
    public IJwtProvider GetProvider()
    {
        // Return the default JWT provider.
        return _defaultJwtProvider;
    }

    /// <summary>
    /// Gets a JWT provider for the specified AWS region.
    /// </summary>
    /// <typeparam name="T">The type of the attribute (must be convertible to RegionEndpoint).</typeparam>
    /// <param name="attribute">The region for which to retrieve a provider.</param>
    /// <returns>
    /// A region-specific JWT provider if one is configured for the requested region;
    /// otherwise, the default JWT provider.
    /// </returns>
    /// <remarks>
    /// This method attempts to retrieve a JWT provider optimized for the specified AWS region.
    /// Using a region-specific provider can improve performance by reducing latency for
    /// cryptographic operations by utilizing KMS in the same region as the caller.
    ///
    /// If no provider is configured for the requested region, the default provider is returned.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the attribute cannot be cast to a RegionEndpoint.
    /// </exception>
    public IJwtProvider GetProvider<T>(
        T attribute)
    {
        // Cast the attribute to a RegionEndpoint.
        var regionEndpoint = attribute as RegionEndpoint
            ?? throw new ArgumentException($"The attribute '{attribute}' is not a valid '{nameof(RegionEndpoint)}'.");

        // Get the JWT provider for the specified region, or the default provider if no region-specific provider is found.
        return _regionalJwtProviders.TryGetValue(regionEndpoint, out var jwtProvider)
            ? jwtProvider
            : _defaultJwtProvider;
    }

    #endregion
}
