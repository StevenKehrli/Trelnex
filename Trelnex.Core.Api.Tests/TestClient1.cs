using Trelnex.Core.Client;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test client that inherits from <see cref="BaseClient"/> and is used to test API endpoints.
/// It provides methods for making HTTP requests to specific test endpoints, such as /delete1, /get1, etc.
/// The client uses a provided <see cref="HttpClient"/> and <see cref="IAccessTokenProvider"/> for making authenticated requests.
/// </summary>
internal class TestClient1(
    HttpClient httpClient,
    IAccessTokenProvider accessTokenProvider)
    : BaseClient(httpClient, accessTokenProvider)
{
    /// <summary>
    /// Sends a DELETE request to the /delete1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> from the /delete1 endpoint.</returns>
    public async Task<TestResponse> Delete()
    {
        // Append "/delete1" to the base address and send the DELETE request.
        return await Delete<TestResponse>(
            uri: BaseAddress.AppendPath("/delete1"));
    }

    /// <summary>
    /// Sends a GET request to the /get1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> from the /get1 endpoint.</returns>
    public async Task<TestResponse> Get()
    {
        // Append "/get1" to the base address and send the GET request.
        return await Get<TestResponse>(
            uri: BaseAddress.AppendPath("/get1"));
    }

    /// <summary>
    /// Sends a PATCH request to the /patch1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> from the /patch1 endpoint.</returns>
    public async Task<TestResponse> Patch()
    {
        // Append "/patch1" to the base address and send the PATCH request with no content.
        return await Patch<string, TestResponse>(
            uri: BaseAddress.AppendPath("/patch1"),
            content: null);
    }

    /// <summary>
    /// Sends a POST request to the /post1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> from the /post1 endpoint.</returns>
    public async Task<TestResponse> Post()
    {
        // Append "/post1" to the base address and send the POST request with no content.
        return await Post<string, TestResponse>(
            uri: BaseAddress.AppendPath("/post1"),
            content: null);
    }

    /// <summary>
    /// Sends a PUT request to the /put1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> from the /put1 endpoint.</returns>
    public async Task<TestResponse> Put()
    {
        // Append "/put1" to the base address and send the PUT request with no content.
        return await Put<string, TestResponse>(
            uri: BaseAddress.AppendPath("/put1"),
            content: null);
    }

    /// <summary>
    /// Sends a GET request to the /queryString endpoint with a query string parameter.
    /// </summary>
    /// <param name="value">The value to pass as a query string parameter.</param>
    /// <returns>A <see cref="TestResponse"/> from the /queryString endpoint.</returns>
    public async Task<TestResponse> QueryString(
        string value)
    {
        // Append "/queryString" to the base string and append "value" to the query string and send the GET request.
        return await Get<TestResponse>(
            uri: BaseAddress.AppendPath("/queryString").AddQueryString("value", value));
    }
}
