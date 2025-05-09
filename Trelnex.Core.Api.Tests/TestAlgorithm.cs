using System.Security.Cryptography;
using JWT.Algorithms;
using Microsoft.IdentityModel.Tokens;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test implementation of the <see cref="IAsymmetricAlgorithm"/> interface using RSA.
/// This class is used in tests to provide a consistent and predictable cryptographic algorithm for signing and verifying JWT tokens.
/// It avoids external dependencies and ensures that authentication tests are isolated and repeatable.
/// </summary>
internal class TestAlgorithm : IAsymmetricAlgorithm
{
    #region Private Static Fields

    private static readonly RsaSecurityKey _securityKey = GetRsaSecurityKey();

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the hash algorithm name.
    /// </summary>
    public HashAlgorithmName HashAlgorithmName => HashAlgorithmName.SHA256;

    /// <summary>
    /// Gets the name of the algorithm.
    /// </summary>
    public string Name => "RS256";

    /// <summary>
    /// Gets the security key used for signing and verifying tokens.
    /// </summary>
    public static SecurityKey SecurityKey => _securityKey;

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Gets an <see cref="RsaSecurityKey"/> for testing purposes.
    /// </summary>
    /// <returns>An <see cref="RsaSecurityKey"/>.</returns>
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
    /// Signs the specified data using the provided key.
    /// </summary>
    /// <param name="key">The key to use for signing.</param>
    /// <param name="bytesToSign">The data to sign.</param>
    /// <returns>The signature.</returns>
    public byte[] Sign(
        byte[] key,
        byte[] bytesToSign)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportParameters(_securityKey.Parameters);

        return rsa.SignData(bytesToSign, HashAlgorithmName, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Verifies the specified signature against the provided data.
    /// </summary>
    /// <param name="bytesToSign">The data that was signed.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <returns><c>true</c> if the signature is valid; otherwise, <c>false</c>.</returns>
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
