using Trelnex.Core.Client;
using Trelnex.Core.Exceptions;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// HTTP client for Amazon's OAuth2 token endpoint.
/// </summary>
/// <remarks>
/// Requests access tokens using AWS SigV4 signatures for authentication.
/// Uses the OAuth2 client credentials flow.
/// </remarks>
/// <param name="httpClient">The HTTP client.</param>
internal class AccessTokenClient(
    HttpClient httpClient)
    : BaseClient(httpClient)
{
    #region Public Methods

    /// <summary>
    /// Requests an access token from Amazon's OAuth2 token endpoint.
    /// </summary>
    /// <param name="principalId">The AWS principal ID.</param>
    /// <param name="signature">The AWS SigV4 signature.</param>
    /// <param name="scope">The requested token scope.</param>
    /// <returns>A new <see cref="AccessToken"/>.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when the token request fails.</exception>
    /// <remarks>
    /// Uses the OAuth2 client credentials flow.
    /// </remarks>
    public async Task<AccessToken> GetAccessToken(
        string principalId,
        CallerIdentitySignature signature,
        string scope)
    {
        // Encode the AWS SigV4 signature to use as the client secret
        var clientSecret = signature.Encode();

        // Create the OAuth2 request parameters as a dictionary
        var nameValueCollection = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", principalId },
            { "client_secret", clientSecret },
            { "scope", scope }
        };

        // Format the dictionary as form URL encoded content
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Send the POST request to the token endpoint and return the access token
        var (response, _) = await PostAsync<FormUrlEncodedContent, AccessToken>(
            relativePath: "/oauth2/token",
            content: content);

        return response;
    }

    #endregion
}
