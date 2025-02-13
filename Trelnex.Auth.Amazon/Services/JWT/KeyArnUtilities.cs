using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Amazon;

namespace Trelnex.Auth.Amazon.Services.JWT;

internal static partial class KeyArnUtilities
{
    /// <summary>
    /// Gets the JWT kid from the key arn.
    /// </summary>
    /// <param name="keyArn">The key arn.</param>
    /// <returns>The JWT kid.</returns>
    public static string? ConvertToKid(
        string keyArn)
    {
        var keyArnBytes = Encoding.ASCII.GetBytes(keyArn);
        var hash = SHA256.HashData(keyArnBytes);

        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Gets the region endpoint from the key arn.
    /// </summary>
    /// <param name="keyArn">The key arn.</param>
    /// <returns>The region endpoint.</returns>
    public static RegionEndpoint? GetRegion(
        string keyArn)
    {
        // get the region, account, and key id
        var match = KeyRegex().Match(keyArn);

        if (match.Success is false) return null;

        // get the region
        var region = match.Groups["region"].Value;

        // get the region endpoint
        return RegionEndpoint.EnumerableAllRegions.FirstOrDefault(re => re.SystemName == region);
    }

    /// <summary>
    /// The regular expression for a key; e.g. arn:aws:kms:us-east-1:571096773025:key/875de039-9e63-4f2c-abae-1877a2f5a4d4
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^arn:aws:kms:(?<region>[a-z\-0-9]+):(?<account>\d{12}):key/(?<keyId>[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$")]
    private static partial Regex KeyRegex();
}
