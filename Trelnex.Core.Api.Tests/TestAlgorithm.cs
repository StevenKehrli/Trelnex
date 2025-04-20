using System.Security.Cryptography;
using JWT.Algorithms;
using Microsoft.IdentityModel.Tokens;

namespace Trelnex.Core.Api.Tests;

internal class TestAlgorithm : IAsymmetricAlgorithm
{
    private static readonly RsaSecurityKey _securityKey = GetRsaSecurityKey();

    public static SecurityKey SecurityKey => _securityKey;

    public string Name => "RS256";

    public HashAlgorithmName HashAlgorithmName => HashAlgorithmName.SHA256;

    public byte[] Sign(byte[] key, byte[] bytesToSign)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportParameters(_securityKey.Parameters);

        return rsa.SignData(bytesToSign, HashAlgorithmName, RSASignaturePadding.Pkcs1);
    }

    public bool Verify(byte[] bytesToSign, byte[] signature)
    {
        using RSA rsa = RSA.Create();

        rsa.ImportParameters(_securityKey.Parameters);

        return rsa.VerifyData(bytesToSign, signature, HashAlgorithmName, RSASignaturePadding.Pkcs1);
    }

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
}
