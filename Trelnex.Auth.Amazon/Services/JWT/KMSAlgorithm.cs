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
using Trelnex.Core.Exceptions;

namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Represents an algorithm to generate and verify JWT signatures using Amazon Key Management Service (KMS).
/// </summary>
/// <remarks>
/// This class implements the <see cref="IAsymmetricAlgorithm"/> interface from the JWT.NET library,
/// allowing AWS KMS keys to be used for JWT signing and verification. It handles the necessary
/// conversions between DER-encoded signatures (AWS KMS format) and ECDSA signatures (JWT format).
///
/// Using AWS KMS for JWT signing offers several advantages:
/// - Private keys never leave the AWS KMS service, enhancing security
/// - Key usage is logged and auditable through AWS CloudTrail
/// - Automatic key rotation can be configured
/// - Fine-grained IAM permissions can control access to signing operations
/// - FIPS 140-2 compliant cryptographic operations
///
/// The class supports ECDSA keys with P-256, P-384, and P-521 curves, which correspond to
/// the ES256, ES384, and ES512 JWT signing algorithms. It automatically handles the conversion
/// between the DER-encoded format used by AWS KMS and the concatenated R|S format used in JWTs.
/// </remarks>
internal class KMSAlgorithm : IAsymmetricAlgorithm
{
    #region Private Fields

    /// <summary>
    /// The Amazon Key Management Service client used for signing and verification operations.
    /// </summary>
    private readonly AmazonKeyManagementServiceClient _client;

    /// <summary>
    /// The name of the hashing algorithm for signing (e.g., SHA-256, SHA-384, SHA-512).
    /// </summary>
    private readonly HashAlgorithmName _hashAlgorithmName;

    /// <summary>
    /// The JSON Web Key representation of the public key used for JWT header information.
    /// </summary>
    private readonly JsonWebKey _jwk;

    /// <summary>
    /// The AWS KMS key ARN used to sign and verify data.
    /// </summary>
    private readonly string _keyArn;

    /// <summary>
    /// The name of the key spec algorithm (e.g., ES256, ES384, ES512).
    /// </summary>
    private readonly string _name;

    /// <summary>
    /// The AWS region endpoint where the KMS key is located.
    /// </summary>
    private readonly RegionEndpoint _regionEndpoint;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="KMSAlgorithm"/> class.
    /// </summary>
    /// <param name="client">The Amazon Key Management Service client.</param>
    /// <param name="regionEndpoint">The AWS region endpoint where the KMS key is located.</param>
    /// <param name="keyArn">The AWS KMS key ARN used for signing and verification.</param>
    /// <param name="name">The algorithm name (e.g., ES256, ES384, ES512).</param>
    /// <param name="hashAlgorithmName">The hashing algorithm name (e.g., SHA-256, SHA-384, SHA-512).</param>
    /// <param name="jwk">The JSON Web Key representation of the public key.</param>
    private KMSAlgorithm(
        AmazonKeyManagementServiceClient client,
        RegionEndpoint regionEndpoint,
        string keyArn,
        string name,
        HashAlgorithmName hashAlgorithmName,
        JsonWebKey jwk)
    {
        // Set the KMS client, region endpoint, key ARN, algorithm name, hashing algorithm name, and JSON Web Key.
        _client = client;
        _regionEndpoint = regionEndpoint;
        _keyArn = keyArn;
        _name = name;
        _hashAlgorithmName = hashAlgorithmName;
        _jwk = jwk;
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="KMSAlgorithm"/> class asynchronously.
    /// </summary>
    /// <param name="credentials">The AWS credentials used to authenticate with KMS. Can be <see langword="null"/> to use the default credential provider chain.</param>
    /// <param name="regionEndpoint">The AWS region endpoint where the KMS key is located.</param>
    /// <param name="keyArn">The AWS KMS key ARN used for signing and verification.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the initialized <see cref="KMSAlgorithm"/> instance.</returns>
    /// <exception cref="AmazonKeyManagementServiceException">Thrown when an error occurs while retrieving the public key from KMS.</exception>
    /// <exception cref="NotSupportedException">Thrown when the key spec is not supported.</exception>
    internal static async Task<KMSAlgorithm> CreateAsync(
        AWSCredentials? credentials,
        RegionEndpoint regionEndpoint,
        string keyArn)
    {
        // Create the KMS client with provided credentials or default credential provider chain.
        var client = new AmazonKeyManagementServiceClient(credentials, regionEndpoint);

        // Get the public key from KMS.
        var request = new GetPublicKeyRequest
        {
            KeyId = keyArn
        };

        var response = await client.GetPublicKeyAsync(request);

        // Get the name of the key spec algorithm (e.g., ES256, ES384, ES512).
        var name = GetAlgorithmName(response.KeySpec);

        // Get the name of the hashing algorithm for signing (e.g., SHA-256, SHA-384, SHA-512).
        // Use the last signing algorithm supported by the key (typically the strongest one).
        var hashAlgorithmName = new HashAlgorithmName(response.SigningAlgorithms.Last());

        // Create the JSON Web Key representation of the public key.
        var jwk = GetJsonWebKey(response);

        // Return a new KMSAlgorithm instance.
        return new KMSAlgorithm(
            client,
            regionEndpoint,
            keyArn,
            name,
            hashAlgorithmName,
            jwk);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the name of the hashing algorithm for signing (e.g., SHA-256, SHA-384, SHA-512).
    /// </summary>
    public HashAlgorithmName HashAlgorithmName => _hashAlgorithmName;

    /// <summary>
    /// Gets the JSON Web Key representation of the public key.
    /// </summary>
    /// <remarks>
    /// This is used for JWT header information and JWKS (JSON Web Key Set) endpoints.
    /// </remarks>
    public JsonWebKey JWK => _jwk;

    /// <summary>
    /// Gets the AWS KMS key ARN used to sign and verify data.
    /// </summary>
    public string KeyArn => _keyArn;

    /// <summary>
    /// Gets the name of the key spec algorithm (e.g., ES256, ES384, ES512).
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the AWS region endpoint where the KMS key is located.
    /// </summary>
    public RegionEndpoint RegionEndpoint => _regionEndpoint;

    #endregion

    #region Public Methods

    /// <summary>
    /// Signs the provided byte array using the AWS KMS key.
    /// </summary>
    /// <param name="key">IGNORED. The key parameter is not used since AWS KMS manages the private key.</param>
    /// <param name="bytesToSign">The data to be signed.</param>
    /// <returns>The ECDSA signature in the format required by JWT (concatenated R and S values).</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when an error occurs while signing the data with KMS.</exception>
    public byte[] Sign(
        byte[] key,
        byte[] bytesToSign)
    {
        try
        {
            // Create a KMS sign request using the key ARN and hashing algorithm.
            var request = new SignRequest()
            {
                KeyId = _keyArn,
                Message = new MemoryStream(bytesToSign),
                MessageType = MessageType.RAW,
                SigningAlgorithm = new SigningAlgorithmSpec(_hashAlgorithmName.Name)
            };

            // Call KMS to sign the bytes.
            var response = _client
                .SignAsync(request)
                .GetAwaiter()
                .GetResult();

            // The signature returned by KMS is a DER-encoded object.
            // Convert it to ECDSA signature format (concatenated R and S values) for JWT.
            return ConvertFromDer(response.Signature);
        }
        catch (AmazonKeyManagementServiceException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Verifies the signature of the bytes using the AWS KMS key.
    /// </summary>
    /// <param name="bytesToSign">The data to verify.</param>
    /// <param name="signature">The signature to verify in JWT format (concatenated R and S values).</param>
    /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when an error occurs while verifying the signature with KMS.</exception>
    public bool Verify(
        byte[] bytesToSign,
        byte[] signature)
    {
        try
        {
            // The signature is in ECDSA signature format (concatenated R and S values).
            // Convert it to DER-encoded format for KMS.
            var derSignature = ConvertToDer(signature);

            // Create a KMS verify request.
            var request = new VerifyRequest()
            {
                KeyId = _keyArn,
                Message = new MemoryStream(bytesToSign),
                Signature = new MemoryStream(derSignature),
                SigningAlgorithm = new SigningAlgorithmSpec(_hashAlgorithmName.Name)
            };

            // Call KMS to verify the signature.
            var response = _client
                .VerifyAsync(request)
                .GetAwaiter()
                .GetResult();

            // Return whether the signature is valid.
            return response.SignatureValid ?? false;
        }
        catch (AmazonKeyManagementServiceException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts a DER-encoded signature (AWS KMS format) to ECDSA signature format (JWT format).
    /// </summary>
    /// <param name="ms">The memory stream containing the DER-encoded signature.</param>
    /// <returns>The ECDSA signature in JWT format (concatenated R and S values).</returns>
    /// <remarks>
    /// The DER-encoded format is a sequence of two integers (r, s) as per ASN.1 DER encoding.
    /// The ECDSA signature format for JWT is simply the concatenation of the R and S values.
    /// Implementation based on https://stackoverflow.com/a/66205185
    /// </remarks>
    private static byte[] ConvertFromDer(
        MemoryStream ms)
    {
        // Parse the DER-encoded object as an ASN.1 sequence.
        var asn1Object = Asn1Object.FromStream(ms) as DerSequence;

        // Extract the R and S integers from the sequence.
        var r = (asn1Object?[0] as DerInteger)!;
        var s = (asn1Object?[1] as DerInteger)!;

        // Concatenate the R and S values into a single byte array.
        return Array.Empty<byte>()
            .Concat(r.Value.ToByteArrayUnsigned())
            .Concat(s.Value.ToByteArrayUnsigned())
            .ToArray();
    }

    /// <summary>
    /// Converts an ECDSA signature (JWT format) to DER-encoded format (AWS KMS format).
    /// </summary>
    /// <param name="signature">The ECDSA signature in JWT format (concatenated R and S values).</param>
    /// <returns>The DER-encoded signature for use with AWS KMS.</returns>
    /// <remarks>
    /// The ECDSA signature format from JWT is the concatenation of the R and S values.
    /// The DER-encoded format required by AWS KMS is a sequence of two integers (r, s) as per ASN.1 DER encoding.
    /// Implementation based on https://stackoverflow.com/a/66205185
    /// </remarks>
    private static byte[] ConvertToDer(
        byte[] signature)
    {
        // Split the signature into R and S components (equal length).
        var halfLength = signature.Length / 2;

        // Extract the R component (first half).
        var rBytes = signature.Take(halfLength).ToArray();
        var r = new BigInteger(1, rBytes);

        // Extract the S component (second half).
        var sBytes = signature.Skip(halfLength).ToArray();
        var s = new BigInteger(1, sBytes);

        // Create a DER sequence containing the R and S integers.
        var derSequence = new DerSequence(
            new DerInteger(r),
            new DerInteger(s)
        );

        // Encode the sequence in DER format.
        return derSequence.GetDerEncoded();
    }

    /// <summary>
    /// Gets the JWT algorithm name from the KMS key specification.
    /// </summary>
    /// <param name="keySpec">The KMS key specification.</param>
    /// <returns>The corresponding JWT algorithm name (e.g., ES256, ES384, ES512).</returns>
    /// <exception cref="NotSupportedException">Thrown when the key specification is not supported.</exception>
    private static string GetAlgorithmName(
        KeySpec keySpec) => keySpec.Value switch
    {
        "ECC_NIST_P256" => nameof(JwtAlgorithmName.ES256),
        "ECC_NIST_P384" => nameof(JwtAlgorithmName.ES384),
        "ECC_NIST_P521" => nameof(JwtAlgorithmName.ES512),
        // If the key specification is not supported, throw an exception.
        _ => throw new NotSupportedException($"The key spec '{keySpec.Value}' is not supported.")
    };

    /// <summary>
    /// Gets the elliptic curve parameters from the KMS key specification.
    /// </summary>
    /// <param name="keySpec">The KMS key specification.</param>
    /// <returns>The corresponding elliptic curve parameters.</returns>
    /// <exception cref="NotSupportedException">Thrown when the key specification is not supported.</exception>
    private static ECCurve GetECCurve(
        KeySpec keySpec) => keySpec.Value switch
    {
        "ECC_NIST_P256" => ECCurve.NamedCurves.nistP256,
        "ECC_NIST_P384" => ECCurve.NamedCurves.nistP384,
        "ECC_NIST_P521" => ECCurve.NamedCurves.nistP521,
         // If the key specification is not supported, throw an exception.
        _ => throw new NotSupportedException($"The key spec '{keySpec.Value}' is not supported.")
    };

    /// <summary>
    /// Creates a JSON Web Key from the KMS public key response.
    /// </summary>
    /// <param name="response">The response from the KMS GetPublicKey request.</param>
    /// <returns>A JSON Web Key representation of the public key for JWT headers and JWKS endpoints.</returns>
    private static JsonWebKey GetJsonWebKey(
        GetPublicKeyResponse response)
    {
        // Get the appropriate elliptic curve parameters for the key.
        var ecCurve = GetECCurve(response.KeySpec);

        // Create an ECDsa instance with the specified curve.
        var publicKey = ECDsa.Create(ecCurve);

        // Import the public key in SubjectPublicKeyInfo format.
        publicKey.ImportSubjectPublicKeyInfo(response.PublicKey.ToArray(), out _);

        // Generate a key ID from the AWS KMS key ARN.
        var kid = KeyArnUtilities.ConvertToKid(response.KeyId);

        // Create a security key from the ECDsa instance.
        var publicSigningKey = new ECDsaSecurityKey(publicKey)
        {
            KeyId = kid
        };

        // Convert the security key to a JSON Web Key.
        return JsonWebKeyConverter.ConvertFromECDsaSecurityKey(publicSigningKey);
    }

    #endregion
}
