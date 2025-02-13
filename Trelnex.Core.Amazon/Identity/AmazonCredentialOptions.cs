namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Represents the configuration options for creating an <see cref="AmazonCredentialProvider"/> instance.
/// </summary>
/// <param name="Region">The region name for the security token service.</param>
public record AmazonCredentialOptions(
    string Region,
    AccessTokenClientConfiguration AccessTokenClient);

/// <summary>
/// Represents the configuration properties for the access token client.
/// </summary>
/// <param name="BaseAddress">The base address <see cref="Uri"/> to build the request <see cref="Uri"/>.</param>
public record AccessTokenClientConfiguration(
    Uri BaseAddress);
