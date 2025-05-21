namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Configuration options for <see cref="AmazonCredentialProvider"/>.
/// </summary>
/// <remarks>
/// Configures the AWS region and token acquisition endpoint.
/// Loaded from appsettings.json under the "Amazon.Credentials" section.
/// </remarks>
/// <param name="Region">The AWS region name.</param>
/// <param name="AccessTokenClient">Configuration for the token acquisition client.</param>
public record AmazonCredentialOptions(
    string Region,
    AccessTokenClientConfiguration AccessTokenClient);

/// <summary>
/// Configuration for the OAuth2 token client.
/// </summary>
/// <remarks>
/// Specifies the endpoint for obtaining access tokens.
/// </remarks>
/// <param name="BaseAddress">The base URI of the token endpoint service.</param>
public record AccessTokenClientConfiguration(
    Uri BaseAddress);
