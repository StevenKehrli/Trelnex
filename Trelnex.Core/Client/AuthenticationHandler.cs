using Trelnex.Core.Identity;

namespace Trelnex.Core.Client;

    /// <summary>
/// HTTP message handler that automatically adds OAuth2 Bearer authentication to outgoing requests.
/// </summary>
/// <param name="accessTokenProvider">The provider used to obtain access tokens for authentication.</param>
/// <remarks>
/// This handler intercepts outgoing HTTP requests and adds the Authorization header with a Bearer token.
/// The token is obtained from the configured access token provider.
/// </remarks>
public class AuthenticationHandler(
    IAccessTokenProvider accessTokenProvider)
    : DelegatingHandler
{
    /// <summary>
    /// Intercepts HTTP requests to add authentication headers before sending.
    /// </summary>
    /// <param name="request">The HTTP request message to authenticate.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The HTTP response message from the downstream handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the access token provider returns a null or empty authorization header.
    /// </exception>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Obtain the authorization header value from the access token provider
        var authorizationHeader = accessTokenProvider.GetAccessToken().GetAuthorizationHeader();

        // Validate that we have a valid authorization header
        // A null or empty header indicates authentication configuration issues
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new InvalidOperationException("The authorization header is null or empty.");
        }

        // Add the Bearer token to the request's Authorization header
        request.Headers.AddAuthorizationHeader(authorizationHeader);

        // Continue with the request pipeline
        return await base.SendAsync(request, cancellationToken);
    }
}
