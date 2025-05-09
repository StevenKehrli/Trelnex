using System.Security.Cryptography;
using JWT.Algorithms;
using Microsoft.IdentityModel.Tokens;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test implementation of the JWT signing algorithm used across the authentication test framework.
///
/// TestAlgorithm performs two critical functions in the test environment:
///
/// 1. Token Signing - As an implementation of IAsymmetricAlgorithm, it provides the Sign method
///    used by TestJwtProvider to cryptographically sign JWT tokens. This creates valid tokens
///    that can be verified by the authentication system.
///
/// 2. Key Provision - It exposes a static SecurityKey property that's used by TestPermission1
///    and TestPermission2 to configure token validation. This ensures that both token creation
///    and validation use the same cryptographic keys.
///
/// This class is fundamental to the authentication test framework, creating a closed system where:
/// - TestJwtProvider uses this algorithm to sign tokens
/// - TestPermission classes use this algorithm's SecurityKey to validate tokens
/// - AuthenticationTests verifies that signature validation works correctly
///
/// By implementing RSA-256 algorithm with a consistent key, it ensures that:
/// - Tokens created by TestJwtProvider have valid signatures
/// - TestPermission classes can properly validate those signatures
/// - Tampered tokens will fail signature validation
///
/// The consistent key approach allows tests to focus on authentication and authorization logic
/// without being affected by external key management or cryptographic complexities.
/// </summary>
internal class TestAlgorithm : IAsymmetricAlgorithm
{
    #region Private Static Fields

    private static readonly RsaSecurityKey _securityKey = GetRsaSecurityKey();

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the hash algorithm name used for token signing and verification.
    ///
    /// SHA-256 is the standard hashing algorithm used in RS256 JWT signing.
    /// This property is used by the Sign method to create token signatures,
    /// ensuring consistent cryptographic operations across the test framework.
    /// </summary>
    public HashAlgorithmName HashAlgorithmName => HashAlgorithmName.SHA256;

    /// <summary>
    /// Gets the name of the JWT algorithm, which is "RS256" (RSA with SHA-256).
    ///
    /// This algorithm name is included in the JWT header to identify the signing algorithm.
    /// RS256 is an asymmetric algorithm that allows tokens to be signed with a private key
    /// and verified with a public key, mimicking real-world JWT validation processes.
    /// </summary>
    public string Name => "RS256";

    /// <summary>
    /// Gets the security key used for both signing tokens and configuring token validation.
    ///
    /// This static property plays a central role in the test authentication framework:
    ///
    /// 1. TestJwtProvider uses this key (via the Sign method) to create token signatures
    ///
    /// 2. TestPermission1 and TestPermission2 use this exact same key in their token
    ///    validation parameters (options.TokenValidationParameters.IssuerSigningKey),
    ///    ensuring that tokens can be properly validated
    ///
    /// This shared key creates a closed cryptographic system where tokens signed by
    /// TestJwtProvider can be validated by the authentication middleware configured
    /// by TestPermission classes.
    /// </summary>
    public static SecurityKey SecurityKey => _securityKey;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates and configures the RSA security key used throughout the test authentication framework.
    ///
    /// This method generates a consistent RSA key pair with the following characteristics:
    ///
    /// 1. 2048-bit Key Size - Strong enough for secure signature creation and validation
    ///
    /// 2. Persistent KeyId - Sets "KeyId.trelnex-auth-amazon-tests-authentication" as the
    ///    key identifier, which matches the keyId parameter passed to TestJwtProvider
    ///
    /// 3. Complete Key Pair - Exports both public and private parameters (true parameter
    ///    to ExportParameters), allowing the key to be used for both signing and validation
    ///
    /// This method is called once during class initialization to create the static _securityKey
    /// field, ensuring the same key is used consistently by all test components.
    /// </summary>
    /// <returns>An RSA security key configured for test JWT signing and validation.</returns>
    public static RsaSecurityKey GetRsaSecurityKey()
    {
        using RSA rsa = RSA.Create();

        rsa.KeySize = 2048;

        var parameters = rsa.ExportParameters(true);

        return new RsaSecurityKey(parameters)
        {
            KeyId = "KeyId.trelnex-auth-amazon-tests-authentication",
        };
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Signs data to create JWT token signatures used in authentication testing.
    ///
    /// This method is called by TestJwtProvider (via JWT.Builder) during token creation to
    /// generate cryptographic signatures for JWT tokens. It uses the static _securityKey
    /// RSA key regardless of the passed-in key parameter, ensuring signature consistency.
    ///
    /// Key aspects of the signing process:
    ///
    /// 1. Key Usage - Uses the private key portion of _securityKey to create signatures
    ///    that can later be verified with the public key portion
    ///
    /// 2. Algorithm Consistency - Uses SHA-256 hashing (from HashAlgorithmName property)
    ///    and PKCS#1 padding to ensure compatibility with standard JWT validation
    ///
    /// 3. Test Integrity - Creates legitimate signatures that will pass validation
    ///    by TestPermission classes, allowing tests to verify proper authorization logic
    ///
    /// This method is essential to creating valid test tokens that can be used to test
    /// both successful authentication and authorization scenarios.
    /// </summary>
    /// <param name="key">The key to use for signing (ignored; _securityKey is used instead).</param>
    /// <param name="bytesToSign">The JWT header and payload data to sign.</param>
    /// <returns>The cryptographic signature bytes for the JWT token.</returns>
    public byte[] Sign(
        byte[] key,
        byte[] bytesToSign)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportParameters(_securityKey.Parameters);

        return rsa.SignData(bytesToSign, HashAlgorithmName, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Verifies JWT token signatures for authentication testing.
    ///
    /// While this method is part of the IAsymmetricAlgorithm interface implementation,
    /// it's not directly used in the test framework. Instead, token validation occurs
    /// through the ASP.NET Core authentication middleware, which uses the SecurityKey
    /// exposed by this class.
    ///
    /// However, the method is still implemented correctly to:
    ///
    /// 1. Provide a complete implementation of the IAsymmetricAlgorithm interface
    ///
    /// 2. Enable direct signature verification if needed for specialized tests
    ///
    /// 3. Maintain symmetry with the Sign method, using the same _securityKey,
    ///    HashAlgorithmName, and padding for consistent cryptographic operations
    ///
    /// This method could be used in specialized tests that need to directly verify
    /// signatures without going through the authentication middleware.
    /// </summary>
    /// <param name="bytesToSign">The original data (JWT header and payload) that was signed.</param>
    /// <param name="signature">The signature bytes to verify against the data.</param>
    /// <returns>True if the signature is valid for the data, false otherwise.</returns>
    public bool Verify(
        byte[] bytesToSign,
        byte[] signature)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportParameters(_securityKey.Parameters);

        return rsa.VerifyData(bytesToSign, signature, HashAlgorithmName, RSASignaturePadding.Pkcs1);
    }

    #endregion
}
