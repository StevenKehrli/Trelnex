using System.Net;
using System.Net.Http.Headers;

namespace Trelnex.Core.Client;

/// <summary>
/// Extension methods for <see cref="HttpRequestHeaders"/>.
/// </summary>
public static class HeadersExtensions
{
    /// <summary>
    /// Adds the Authorization Header to the <see cref="HttpRequestHeaders"/>.
    /// </summary>
    /// <param name="headers">The specified <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="authorizationHeader">The value of the Authorization Header.</param>
    /// <returns>The <see cref="HttpRequestHeaders"/>.</returns>
    public static HttpRequestHeaders AddAuthorizationHeader(
        this HttpRequestHeaders headers,
        string authorizationHeader)
    {
        headers.Add(
            name: HttpRequestHeader.Authorization.ToString(),
            value: authorizationHeader);

        return headers;
    }
}
