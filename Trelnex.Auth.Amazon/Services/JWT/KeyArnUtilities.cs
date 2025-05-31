using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Amazon;

namespace Trelnex.Auth.Amazon.Services.JWT;

/// <summary>
/// Provides utility methods for working with AWS KMS key ARNs (Amazon Resource Names).
/// </summary>
/// <remarks>
/// This utility class offers methods for parsing and transforming AWS KMS key ARNs,
/// which are used to uniquely identify AWS KMS keys across AWS accounts and regions.
///
/// An AWS KMS key ARN follows this format:
/// arn:aws:kms:{region}:{account}:key/{key-id}
///
/// Example: arn:aws:kms:us-east-1:123456789012:key/abcd1234-ab12-cd34-ef56-abcdef123456
///
/// These utilities help with:
/// - Extracting region information from ARNs
/// - Converting ARNs to JWT key IDs (kid) for use in JWT headers
/// - Validating ARN syntax
/// </remarks>
internal static partial class KeyArnUtilities
{
    #region Public Static Methods

    /// <summary>
    /// Converts an AWS KMS key ARN to a JWT key ID (kid) using a cryptographic hash.
    /// </summary>
    /// <param name="keyArn">The AWS KMS key ARN to convert.</param>
    /// <returns>
    /// A base64-encoded SHA-256 hash of the key ARN, suitable for use as a JWT key ID (kid).
    /// </returns>
    /// <remarks>
    /// This method creates a deterministic, fixed-length identifier for an AWS KMS key
    /// that can be used in the JWT header's 'kid' parameter. Using a hash of the ARN
    /// ensures a consistent identifier that doesn't expose the actual ARN information
    /// while still providing a unique and repeatable value.
    ///
    /// The hash is encoded using base64 to make it URL-safe and compliant with common
    /// JWT library expectations for kid values.
    /// </remarks>
    public static string? ConvertToKid(
        string keyArn)
    {
        // Convert the key ARN to a byte array.
        var keyArnBytes = Encoding.ASCII.GetBytes(keyArn);

        // Hash the byte array using SHA256.
        var hash = SHA256.HashData(keyArnBytes);

        // Return the base64-encoded hash.
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Extracts the AWS region endpoint from a KMS key ARN.
    /// </summary>
    /// <param name="keyArn">The AWS KMS key ARN to parse.</param>
    /// <returns>
    /// The corresponding <see cref="RegionEndpoint"/> if the ARN is valid and contains a recognized region;
    /// otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// This method parses an AWS KMS key ARN to extract the region identifier and then
    /// maps it to the corresponding AWS SDK RegionEndpoint object. This is useful for
    /// determining which AWS region should be used for KMS API calls involving this key.
    ///
    /// If the ARN doesn't match the expected format or contains an unrecognized region,
    /// the method returns null.
    /// </remarks>
    public static RegionEndpoint? GetRegion(
        string keyArn)
    {
        // Get the region, account, and key id.
        var match = KeyRegex().Match(keyArn);

        // If the ARN is invalid, return null.
        if (match.Success is false) return null;

        // Get the region.
        var region = match.Groups["region"].Value;

        // Get the region endpoint.
        return RegionEndpoint.EnumerableAllRegions.FirstOrDefault(re => re.SystemName == region);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Returns a regular expression for validating and parsing AWS KMS key ARNs.
    /// </summary>
    /// <returns>A compiled regular expression for KMS key ARNs.</returns>
    /// <remarks>
    /// This method provides a regular expression that matches AWS KMS key ARNs in the format:
    /// arn:aws:kms:{region}:{account}:key/{key-id}
    ///
    /// The expression captures three named groups:
    /// - "region": The AWS region identifier (e.g., "us-east-1")
    /// - "account": The 12-digit AWS account number
    /// - "keyId": The UUID of the KMS key
    ///
    /// Example of a matching ARN:
    /// arn:aws:kms:us-east-1:123456789012:key/abcd1234-ab12-cd34-ef56-abcdef123456
    ///
    /// This is implemented using C# source generators to create an efficient compiled regex.
    /// </remarks>
    [GeneratedRegex(@"^arn:aws:kms:(?<region>[a-z\-0-9]+):(?<account>\d{12}):key/(?<keyId>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$")]
    private static partial Regex KeyRegex();

    #endregion
}
