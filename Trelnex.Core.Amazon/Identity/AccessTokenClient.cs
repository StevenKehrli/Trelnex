using Trelnex.Core.Client;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Amazon.Identity;

/// <summary>
/// Initializes a new instance of the <see cref="AccessTokenClient"/>.
/// </summary>
/// <param name="httpClient">The specified <see cref="HttpClient"/> instance.</param>
internal class AccessTokenClient(
    HttpClient httpClient)
    : BaseClient(httpClient)
{
    /// <summary>
    /// Get the access token for the specified principal and scope.
    /// </summary>
    /// <param name="principalId">The specified principal id.</param>
    /// <param name="signature">The signature to identify the caller identity through an AWS sigv4 (region and headers).</param>
    /// <param name="scope">The specified scope.</param>
    /// <returns>The new <see cref="AccessToken"/>.</returns>
    public async Task<AccessToken> GetAccessToken(
        string principalId,
        CallerIdentitySignature signature,
        string scope)
    {
        // create the form content
        var clientSecret = signature.Encode();

        var nameValueCollection = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", principalId },
            { "client_secret", clientSecret },
            { "scope", scope }
        };

        var content = new FormUrlEncodedContent(nameValueCollection);

        return await Post<FormUrlEncodedContent, AccessToken>(
            uri: BaseAddress.AppendPath("/oauth2/token"),
            content: content);
    }
}
