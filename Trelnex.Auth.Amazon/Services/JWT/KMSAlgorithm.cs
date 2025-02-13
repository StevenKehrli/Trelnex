using System.Net;
using System.Security.Cryptography;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Amazon.Runtime;
using JWT.Algorithms;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Math;
using Trelnex.Core;

namespace Trelnex.Auth.Amazon.Services.JWT;

public interface IKMSAlgorithm : IAsymmetricAlgorithm
{
    /// <summary>
    /// get the json web key id
    /// </summary>
    JsonWebKey JWK { get; }
}

/// <summary>
/// Represents an algorithm to generate a JWT signature using Amazon Key Management Service.
/// </summary>
internal class KMSAlgorithm : IKMSAlgorithm
{
    /// <summary>
    /// The Amazon Key Management Service client
    /// </summary>
    private readonly AmazonKeyManagementServiceClient _client;

    /// <summary>
    /// The name of the hashing algorithm for signing (e.g. SHA-256, SHA-384, SHA-512).
    /// </summary>
    private readonly HashAlgorithmName _hashAlgorithmName;

    /// <summary>
    /// The json web key representation of the public key
    /// </summary>
    private readonly JsonWebKey _jwk;

    /// <summary>
    /// The AWS KMS key arn used to sign the data
    /// </summary>
    private readonly string _keyArn;

    /// <summary>
    /// The name of the key spec algorithm (e.g. ECC_NIST_P256, ECC_NIST_P384, ECC_NIST_P521).
    /// </summary>
    private readonly string _name;

    /// <summary>
    /// The AWS region endpoint
    /// </summary>
    private readonly RegionEndpoint _region;

    /// <summary>
    /// Initializes a new instance of the <see cref="KMSAlgorithm"/> class.
    /// </summary>
    /// <param name="client">The Amazon Key Management Service client</param>
    /// <param name="region">The region endpoint</param>
    /// <param name="keyArn">The key arn used to sign the data</param>
    /// <param name="name">The algorithm name</param>
    /// <param name="hashAlgorithmName">The hashing algorithm name</param>
    private KMSAlgorithm(
        AmazonKeyManagementServiceClient client,
        RegionEndpoint region,
        string keyArn,
        string name,
        HashAlgorithmName hashAlgorithmName,
        JsonWebKey jwk)
    {
        _client = client;
        _region = region;
        _keyArn = keyArn;
        _name = name;
        _hashAlgorithmName = hashAlgorithmName;
        _jwk = jwk;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="KMSAlgorithm"/> class.
    /// </summary>
    /// <param name="credentials">AWS Credentials</param>
    /// <param name="region">The region endpoint.</param>
    /// <param name="keyArn">The key arn.</param>
    /// <returns>The <see cref="KMSAlgorithm"/> instance</returns>
    internal static async Task<KMSAlgorithm> CreateAsync(
        AWSCredentials? credentials,
        RegionEndpoint region,
        string keyArn)
    {
        // create the client
        var client = new AmazonKeyManagementServiceClient(credentials, region);

        // get the public key
        var request = new GetPublicKeyRequest
        {
            KeyId = keyArn
        };

        var response = await client.GetPublicKeyAsync(request);

        // get the name of the key spec algorithm name
        var name = GetAlgorithmName(response.KeySpec);

        // get the name of the hashing algorithm for signing
        var hashAlogithmName = new HashAlgorithmName(response.SigningAlgorithms.Last());

        // get the jwk
        var jwk = GetJsonWebKey(response);

        return new KMSAlgorithm(
            client,
            region,
            keyArn,
            name,
            hashAlogithmName,
            jwk);
    }

    /// <summary>
    /// Gets the name of the hashing algorithm for signing (e.g. SHA-256, SHA-384, SHA-512).
    /// </summary>
    public HashAlgorithmName HashAlgorithmName => _hashAlgorithmName;

    /// <summary>
    /// Gets the json web key representation of the public key.
    /// </summary>
    public JsonWebKey JWK => _jwk;

    /// <summary>
    /// Gets the AWS KMS key arn used to sign the data.
    /// </summary>
    public string KeyArn => _keyArn;

    /// <summary>
    /// Gets the name of the key spec algorithm (e.g. ECC_NIST_P256, ECC_NIST_P384, ECC_NIST_P521).
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the AWS region endpoint.
    /// </summary>
    public RegionEndpoint Region => _region;

    /// <summary>
    /// Signs provided byte array with provided key.
    /// </summary>
    /// <param name="key">IGNORED. The key used to sign the data</param>
    /// <param name="bytesToSign">The data to sign</param>
    /// <exception cref="HttpStatusCodeException">The error that occurred while signing the bytes</exception>
    public byte[] Sign(
        byte[] key,
        byte[] bytesToSign)
    {
        try
        {
            // call KMS to sign the bytes
            var request = new SignRequest()
            {
                KeyId = _keyArn,
                Message = new MemoryStream(bytesToSign),
                MessageType = MessageType.RAW,
                SigningAlgorithm = new SigningAlgorithmSpec(_hashAlgorithmName.Name)
            };

            var response = _client.SignAsync(request).Result;

            // the signature is a DER-encoded object
            // convert to ecdsa signature (aka r|s aka jose-style)
            return ConvertFromDer(response.Signature);
        }
        catch (AmazonKeyManagementServiceException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Verifies the signature of the bytes.
    /// </summary>
    /// <param name="bytesToSign">The data to verify</param>
    /// <param name="signature">The signature to verify</param>
    /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="HttpStatusCodeException">The error that occurred while verifying the signature</exception>
    public bool Verify(
        byte[] bytesToSign,
        byte[] signature)
    {
        try
        {
            // the signature is an ecdsa signature (aka r|s aka jose-style)
            // convert to  DER-encoded object
            var derSignature = ConvertToDer(signature);

            // call KMS to verify the signature
            var request = new VerifyRequest()
            {
                KeyId = _keyArn,
                Message = new MemoryStream(bytesToSign),
                Signature = new MemoryStream(derSignature),
                SigningAlgorithm = new SigningAlgorithmSpec(_hashAlgorithmName.Name)
            };

            var response = _client.VerifyAsync(request).Result;

            return response.SignatureValid;
        }
        catch (AmazonKeyManagementServiceException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Converts the DER-encoded object to ecdsa signature (aka r|s aka jose-style).
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://stackoverflow.com/a/66205185
    /// </para>
    /// </remarks>
    /// <param name="ms">The memory stream containing the DER-encoded object</param>
    /// <returns>The ecdsa signature (aka r|s aka jose-style)</returns>
    private static byte[] ConvertFromDer(
        MemoryStream ms)
    {
        // https://stackoverflow.com/a/66205185
        // the signature is a DER-encoded object
        // convert to ecdsa signature (aka r|s aka jose-style)
        var asn1Object = Asn1Object.FromStream(ms) as DerSequence;

        var r = (asn1Object?[0] as DerInteger)!;
        var s = (asn1Object?[1] as DerInteger)!;

        return Array.Empty<byte>()
            .Concat(r.Value.ToByteArrayUnsigned())
            .Concat(s.Value.ToByteArrayUnsigned())
            .ToArray();
    }

    /// <summary>
    /// Converts the ecdsa signature (aka r|s aka jose-style) to DER-encoded object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// https://stackoverflow.com/a/66205185
    /// </para>
    /// </remarks>
    /// <param name="signature">The ecdsa signature (aka r|s aka jose-style)</param>
    /// <returns>The DER-encoded object</returns>
    private static byte[] ConvertToDer(
        byte[] signature)
    {
        var halfLength = signature.Length / 2;

        var rBytes = signature.Take(halfLength).ToArray();
        var r = new BigInteger(1, rBytes);

        var sBytes = signature.Skip(halfLength).ToArray();
        var s = new BigInteger(1, sBytes);

        var derSequence = new DerSequence(
            new DerInteger(r),
            new DerInteger(s)
        );

        return derSequence.GetDerEncoded();
    }

    /// <summary>
    /// Get the JWT alg name from the key spec.
    /// </summary>
    /// <param name="keySpec">The key spec</param>
    /// <returns>The JWT alg name</returns>
    /// <exception cref="NotSupportedException">The key spec is not supported</exception>
    private static string GetAlgorithmName(
        KeySpec keySpec) => keySpec.Value switch
    {
        "ECC_NIST_P256" => nameof(JwtAlgorithmName.ES256),
        "ECC_NIST_P384" => nameof(JwtAlgorithmName.ES384),
        "ECC_NIST_P521" => nameof(JwtAlgorithmName.ES512),
        _ => throw new NotSupportedException($"The key spec '{keySpec.Value}' is not supported.")
    };

    /// <summary>
    /// Get the ECC curve from the key spec.
    /// </summary>
    /// <param name="keySpec">The key spec</param>
    /// <returns>The ECC curve</returns>
    /// <exception cref="NotSupportedException">The key spec is not supported</exception>
    private static ECCurve GetECCurve(
        KeySpec keySpec) => keySpec.Value switch
    {
        "ECC_NIST_P256" => ECCurve.NamedCurves.nistP256,
        "ECC_NIST_P384" => ECCurve.NamedCurves.nistP384,
        "ECC_NIST_P521" => ECCurve.NamedCurves.nistP521,
        _ => throw new NotSupportedException($"The key spec '{keySpec.Value}' is not supported.")
    };

    /// <summary>
    /// Get the json web key from the public key.
    /// </summary>
    /// <param name="response">The response from the GetPublicKey request</param>
    /// <returns>The json web key</returns>
    private static JsonWebKey GetJsonWebKey(
        GetPublicKeyResponse response)
    {
        // get the eliptic curve
        var ecCurve = GetECCurve(response.KeySpec);

        // get the public key
        var publicKey = ECDsa.Create(ecCurve);

        publicKey.ImportSubjectPublicKeyInfo(response.PublicKey.ToArray(), out _);

        // get the jwt kid
        var kid = KeyArnUtilities.ConvertToKid(response.KeyId);

        // get the public signing key
        var publicSigningKey = new ECDsaSecurityKey(publicKey)
        {
            KeyId = kid
        };

        // convert the public signing key to a json web key
        return JsonWebKeyConverter.ConvertFromECDsaSecurityKey(publicSigningKey);
    }
}
