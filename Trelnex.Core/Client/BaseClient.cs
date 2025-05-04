using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Trelnex.Core.Identity;

namespace Trelnex.Core.Client;

/// <summary>
/// Base class for HTTP API clients providing standardized request handling.
/// </summary>
/// <param name="httpClient">The HTTP client for sending requests.</param>
/// <param name="accessTokenProvider">Optional provider for authentication tokens.</param>
public abstract class BaseClient(
    HttpClient httpClient,
    IAccessTokenProvider? accessTokenProvider = null)
{
    /// <summary>
    /// JSON serialization options used for request and response content.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        // Configure JSON serialization to omit null values and use relaxed character escaping.
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Gets the base <see cref="Uri"/> for all requests made by this client.
    /// </summary>
    /// <returns>The base URI used to build request URIs.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the BaseAddress is not configured in the HttpClient.
    /// </exception>
    protected Uri BaseAddress => httpClient.BaseAddress ?? throw new ConfigurationErrorsException("BaseAddress is not set.");

    /// <summary>
    /// Sends a DELETE request to remove a resource.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="uri">The URI of the resource to delete.</param>
    /// <param name="addHeaders">Optional callback to add custom headers to the request.</param>
    /// <param name="errorHandler">Optional callback to process error responses.</param>
    /// <returns>The deserialized response object.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code.
    /// </exception>
    protected async Task<TResponse> Delete<TResponse>(
        Uri uri,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
    {
        // Call the SendRequest method with the DELETE HTTP method.
        return await SendRequest<object, TResponse>(
            httpMethod: HttpMethod.Delete,
            uri: uri,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Sends a GET request to retrieve a resource.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="uri">The URI of the resource to retrieve.</param>
    /// <param name="addHeaders">Optional callback to add custom headers to the request.</param>
    /// <param name="errorHandler">Optional callback to process error responses.</param>
    /// <returns>The deserialized response object.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code.
    /// </exception>
    protected async Task<TResponse> Get<TResponse>(
        Uri uri,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
    {
        // Call the SendRequest method with the GET HTTP method.
        return await SendRequest<object, TResponse>(
            httpMethod: HttpMethod.Get,
            uri: uri,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Sends a PATCH request to partially update a resource.
    /// </summary>
    /// <typeparam name="TRequest">The request body type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="uri">The URI of the resource to update.</param>
    /// <param name="content">The partial modifications to apply.</param>
    /// <param name="addHeaders">Optional callback to add custom headers to the request.</param>
    /// <param name="errorHandler">Optional callback to process error responses.</param>
    /// <returns>The deserialized response object.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code.
    /// </exception>
    protected async Task<TResponse> Patch<TRequest, TResponse>(
        Uri uri,
        TRequest? content,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        // Call the SendRequest method with the PATCH HTTP method.
        return await SendRequest<TRequest, TResponse>(
            httpMethod: HttpMethod.Patch,
            uri: uri,
            content: content,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Sends a POST request to create a new resource.
    /// </summary>
    /// <typeparam name="TRequest">The request body type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="uri">The URI endpoint for creating the resource.</param>
    /// <param name="content">The data for the new resource.</param>
    /// <param name="addHeaders">Optional callback to add custom headers to the request.</param>
    /// <param name="errorHandler">Optional callback to process error responses.</param>
    /// <returns>The deserialized response object.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code.
    /// </exception>
    protected async Task<TResponse> Post<TRequest, TResponse>(
        Uri uri,
        TRequest? content,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        // Call the SendRequest method with the POST HTTP method.
        return await SendRequest<TRequest, TResponse>(
            httpMethod: HttpMethod.Post,
            uri: uri,
            content: content,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Sends a PUT request to create or completely replace a resource.
    /// </summary>
    /// <typeparam name="TRequest">The request body type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="uri">The URI of the resource to create or replace.</param>
    /// <param name="content">The complete representation of the resource.</param>
    /// <param name="addHeaders">Optional callback to add custom headers to the request.</param>
    /// <param name="errorHandler">Optional callback to process error responses.</param>
    /// <returns>The deserialized response object.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code.
    /// </exception>
    protected async Task<TResponse> Put<TRequest, TResponse>(
        Uri uri,
        TRequest? content,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        // Call the SendRequest method with the PUT HTTP method.
        return await SendRequest<TRequest, TResponse>(
            httpMethod: HttpMethod.Put,
            uri: uri,
            content: content,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Core method that sends HTTP requests and processes responses.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
    /// <param name="httpMethod">The HTTP method to use (GET, POST, etc.).</param>
    /// <param name="uri">The target URI for the request.</param>
    /// <param name="content">Optional request body content.</param>
    /// <param name="addHeaders">Optional callback for adding custom headers.</param>
    /// <param name="errorHandler">Optional callback for custom error processing.</param>
    /// <returns>The deserialized response object of type <typeparamref name="TResponse"/>.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when a non-success status code is received, with details about the error.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when authentication is required but the token is unavailable.
    /// </exception>
    private async Task<TResponse> SendRequest<TRequest, TResponse>(
        HttpMethod httpMethod,
        Uri uri,
        TRequest? content = null,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        var clientName = GetType().FullName;

        // Build our request message.
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = uri,
            Headers =
            {
                { HttpRequestHeader.Accept.ToString(), MediaTypeNames.Application.Json }
            }
        };

        // Add any additional headers.
        addHeaders?.Invoke(httpRequestMessage.Headers);

        // Add the authorization header if we have a token provider.
        if (accessTokenProvider is not null)
        {
            var authorizationHeader = accessTokenProvider.GetAccessToken().GetAuthorizationHeader();

            // Add the authorization header to the request.
            // If the header is null, we don't have a token.
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                throw new InvalidOperationException("The authorization header is null or empty.");
            }

            httpRequestMessage.Headers.AddAuthorizationHeader(authorizationHeader);
        }

        // If there is content, serialize it to the request body.
        if (content is not null)
        {
            // If the content is already an HttpContent, use it directly; otherwise, serialize it as JSON.
            _ = (content is HttpContent httpContent)
                ? httpRequestMessage.Content = httpContent
                : httpRequestMessage.Content = JsonContent.Create(
                    inputValue: content,
                    options: _options);
        }

        // Send the request and get the response.
        using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

        // Read the response content as string.
        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

        // If the response is not successful, handle the error.
        if (httpResponseMessage.IsSuccessStatusCode is false)
        {
            // Format an error message and default to the status code.
            var message = httpResponseMessage.StatusCode.ToReason();

            // If there is no response content or error handler, there is nothing further we can do.
            if (string.IsNullOrWhiteSpace(responseContent) || errorHandler is null)
            {
                throw new HttpStatusCodeException(
                    httpStatusCode: httpResponseMessage.StatusCode,
                    message: $"{clientName}: {message}");
            }

            try
            {
                // Try to override the message with the error handler.
                var jsonNode = JsonSerializer.Deserialize<JsonNode>(responseContent);

                if (jsonNode is not null)
                {
                    message = errorHandler(jsonNode) ?? message;
                }

                throw new HttpStatusCodeException(
                    httpStatusCode: httpResponseMessage.StatusCode,
                    message: $"{clientName}: {message}");
            }
            catch
            {
                // If there is an exception during error handling, throw a default HttpStatusCodeException.
                throw new HttpStatusCodeException(
                    httpStatusCode: httpResponseMessage.StatusCode,
                    message: $"{clientName}: {message}");
            }
        }

        // If the response content is empty, return the default value for the response type.
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default!;
        }

        try
        {
            // Try to deserialize the response content (string) as TResponse.
            var response = JsonSerializer.Deserialize<TResponse>(responseContent, _options);

            // If the response is not null, return it.
            if (response is not null) return response;

            // If the response is null, throw an HttpStatusCodeException.
            throw new HttpStatusCodeException(
                httpStatusCode: HttpStatusCode.UnprocessableContent,
                message: responseContent);
        }
        catch
        {
            // If there is an exception during deserialization, throw an HttpStatusCodeException.
            throw new HttpStatusCodeException(
                httpStatusCode: HttpStatusCode.UnprocessableContent,
                message: responseContent);
        }
    }
}
