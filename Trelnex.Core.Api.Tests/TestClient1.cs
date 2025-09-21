using Trelnex.Core.Client;

namespace Trelnex.Core.Api.Tests;

/// <summary>
/// A test client that inherits from <see cref="BaseClient"/> and is used to test API endpoints.
///
/// This client provides methods for making HTTP requests to specific test endpoints defined in
/// BaseApiTests.cs, such as /delete1, /get1, etc. Each method corresponds to a different HTTP
/// method (GET, POST, PUT, DELETE, PATCH) and deserializes the response into a <see cref="TestResponse"/> object.
///
/// The TestResponse objects returned by these endpoints have a Message property containing a unique
/// identifier string that allows tests to verify they received the expected response from the correct endpoint.
/// The client methods are used by ClientTests.cs to test both authentication and HTTP method handling.
///
/// The client uses a provided <see cref="HttpClient"/> for making authenticated requests,
/// allowing tests to verify both authorized and unauthorized scenarios.
/// </summary>
internal class TestClient1(
    HttpClient httpClient)
    : BaseClient(httpClient)
{
    /// <summary>
    /// Sends a DELETE request to the /delete1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> with a Message property value of "delete1" to verify the endpoint.</returns>
    public async Task<TestResponse> DeleteAsync()
    {
        // Append "/delete1" to the base address and send the DELETE request.
        var (response, _) = await DeleteAsync<TestResponse>(
            relativePath: "/delete1",
            cancellationToken: CancellationToken.None);

        return response;
    }

    /// <summary>
    /// Sends a GET request to the /get1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> with a Message property value of "get1" to verify the endpoint.</returns>
    public async Task<TestResponse> GetAsync()
    {
        // Append "/get1" to the base address and send the GET request.
        var (response, _) = await GetAsync<TestResponse>(
            relativePath: "/get1",
            cancellationToken: CancellationToken.None);

        return response;
    }

    /// <summary>
    /// Sends a PATCH request to the /patch1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> with a Message property value of "patch1" to verify the endpoint.</returns>
    public async Task<TestResponse> PatchAsync()
    {
        // Append "/patch1" to the base address and send the PATCH request with no content.
        var (response, _) = await PatchAsync<string, TestResponse>(
            relativePath: "/patch1",
            content: null,
            cancellationToken: CancellationToken.None);

        return response;
    }

    /// <summary>
    /// Sends a POST request to the /post1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> with a Message property value of "post1" to verify the endpoint.</returns>
    public async Task<TestResponse> PostAsync()
    {
        // Append "/post1" to the base address and send the POST request with no content.
        var (response, _) = await PostAsync<string, TestResponse>(
            relativePath: "/post1",
            content: null,
            cancellationToken: CancellationToken.None);

        return response;
    }

    /// <summary>
    /// Sends a PUT request to the /put1 endpoint.
    /// </summary>
    /// <returns>A <see cref="TestResponse"/> with a Message property value of "put1" to verify the endpoint.</returns>
    public async Task<TestResponse> PutAsync()
    {
        // Append "/put1" to the base address and send the PUT request with no content.
        var (response, _) = await PutAsync<string, TestResponse>(
            relativePath: "/put1",
            content: null,
            cancellationToken: CancellationToken.None);

        return response;
    }

    /// <summary>
    /// Sends a GET request to the /queryString endpoint with a query string parameter.
    /// </summary>
    /// <param name="value">The value to pass as a query string parameter.</param>
    /// <returns>A <see cref="TestResponse"/> with a Message property value matching the input parameter,
    /// demonstrating that query string parameters are correctly received and processed.</returns>
    public async Task<TestResponse> QueryStringAsync(
        string value)
    {
        // Append "/queryString" to the base string and append "value" to the query string and send the GET request.
        var relativePath = "/queryString"
            .AddQueryString(("value", value));

        var (response, _) = await GetAsync<TestResponse>(
            relativePath: relativePath,
            cancellationToken: CancellationToken.None);

        return response;
    }
}
