using System.Net;
using System.Net.Http.Headers;

namespace Trelnex.Core.Client;

/// <summary>
/// Extension methods for handling HTTP request headers.
/// </summary>
public static class HeadersExtensions
{
    /// <summary>
    /// Adds an Authorization header to the HTTP request headers.
    /// </summary>
    /// <param name="headers">The HTTP request headers collection to modify.</param>
    /// <param name="authorizationHeader">The complete authorization header value.</param>
    /// <returns>The modified headers collection for method chaining.</returns>
    public static HttpRequestHeaders AddAuthorizationHeader(
        this HttpRequestHeaders headers,
        string authorizationHeader)
    {
        // Add the authorization header to the request headers.
        headers.Add(
            name: HttpRequestHeader.Authorization.ToString(),
            value: authorizationHeader);

        // Return the modified headers collection for method chaining.
        return headers;
    }
}
