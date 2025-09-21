using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Trelnex.Core.Exceptions;

namespace Trelnex.Core.Client;

/// <summary>
/// Base class for HTTP API clients providing standardized request handling with structured error processing.
/// Handles JSON serialization/deserialization, error response parsing, and common HTTP operations.
/// </summary>
/// <param name="httpClient">The configured HTTP client for sending requests.</param>
public abstract class BaseClient(
    HttpClient httpClient)
{
    #region Private Static Fields

    /// <summary>
    /// Shared JSON serialization options for consistent request/response processing.
    /// Configured to ignore null values during serialization.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion

    #region Private Properties

    /// <summary>
    /// Gets the base URI for all HTTP requests made by this client.
    /// </summary>
    /// <returns>The configured base address from the HttpClient.</returns>
    /// <exception cref="ConfigurationErrorsException">
    /// Thrown when the BaseAddress is not configured in the HttpClient.
    /// </exception>
    private Uri _baseAddress => httpClient.BaseAddress ?? throw new ConfigurationErrorsException("BaseAddress is not set.");

    #endregion

    #region Protected Methods

    /// <summary>
    /// Sends a DELETE request to remove a resource.
    /// </summary>
    /// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
    /// <param name="relativePath">The relative path of the resource to delete.</param>
    /// <param name="addRequestHeaders">Optional callback to add custom headers (e.g., authorization, correlation IDs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the HTTP request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the deserialized response and HTTP response headers.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code or response processing fails.
    /// </exception>
    protected async Task<(TResponse response, HttpResponseHeaders headers)> DeleteAsync<TResponse>(
        string relativePath,
        Action<HttpRequestHeaders>? addRequestHeaders = null,
        CancellationToken cancellationToken = default) =>

        await SendRequestAsync<object, TResponse>(
            httpMethod: HttpMethod.Delete,
            relativePath: relativePath,
            addRequestHeaders: addRequestHeaders,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Sends a GET request to retrieve a resource.
    /// </summary>
    /// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
    /// <param name="relativePath">The relative path of the resource to retrieve.</param>
    /// <param name="addRequestHeaders">Optional callback to add custom headers (e.g., authorization, correlation IDs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the HTTP request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the deserialized response and HTTP response headers.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code or response processing fails.
    /// </exception>
    protected async Task<(TResponse response, HttpResponseHeaders headers)> GetAsync<TResponse>(
        string relativePath,
        Action<HttpRequestHeaders>? addRequestHeaders = null,
        CancellationToken cancellationToken = default) =>

        await SendRequestAsync<object, TResponse>(
            httpMethod: HttpMethod.Get,
            relativePath: relativePath,
            addRequestHeaders: addRequestHeaders,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Sends a PATCH request to partially update a resource with the provided modifications.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body containing partial updates.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
    /// <param name="relativePath">The relative path of the resource to update.</param>
    /// <param name="content">The partial modifications to apply to the resource.</param>
    /// <param name="addRequestHeaders">Optional callback to add custom headers (e.g., authorization, correlation IDs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the HTTP request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the deserialized response and HTTP response headers.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code or response processing fails.
    /// </exception>
    protected async Task<(TResponse response, HttpResponseHeaders headers)> PatchAsync<TRequest, TResponse>(
        string relativePath,
        TRequest? content,
        Action<HttpRequestHeaders>? addRequestHeaders = null,
        CancellationToken cancellationToken = default)
        where TRequest : class =>

        await SendRequestAsync<TRequest, TResponse>(
            httpMethod: HttpMethod.Patch,
            relativePath: relativePath,
            content: content,
            addRequestHeaders: addRequestHeaders,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Sends a POST request to create a new resource with the provided data.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body containing the new resource data.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
    /// <param name="relativePath">The relative path endpoint for creating the resource.</param>
    /// <param name="content">The data for the new resource to be created.</param>
    /// <param name="addRequestHeaders">Optional callback to add custom headers (e.g., authorization, correlation IDs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the HTTP request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the deserialized response and HTTP response headers.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code or response processing fails.
    /// </exception>
    protected async Task<(TResponse response, HttpResponseHeaders headers)> PostAsync<TRequest, TResponse>(
        string relativePath,
        TRequest? content,
        Action<HttpRequestHeaders>? addRequestHeaders = null,
        CancellationToken cancellationToken = default)
        where TRequest : class =>

        await SendRequestAsync<TRequest, TResponse>(
            httpMethod: HttpMethod.Post,
            relativePath: relativePath,
            content: content,
            addRequestHeaders: addRequestHeaders,
            cancellationToken: cancellationToken);

    /// <summary>
    /// Sends a PUT request to create or completely replace a resource with the provided data.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body containing the complete resource representation.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the response into.</typeparam>
    /// <param name="relativePath">The relative path of the resource to create or replace.</param>
    /// <param name="content">The complete representation of the resource (replaces existing data entirely).</param>
    /// <param name="addRequestHeaders">Optional callback to add custom headers (e.g., authorization, correlation IDs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the HTTP request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized response and HTTP response headers.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when the server returns a non-success status code or response processing fails.
    /// </exception>
    protected async Task<(TResponse response, HttpResponseHeaders headers)> PutAsync<TRequest, TResponse>(
        string relativePath,
        TRequest? content,
        Action<HttpRequestHeaders>? addRequestHeaders = null,
        CancellationToken cancellationToken = default)
        where TRequest : class =>

        await SendRequestAsync<TRequest, TResponse>(
            httpMethod: HttpMethod.Put,
            relativePath: relativePath,
            content: content,
            addRequestHeaders: addRequestHeaders,
            cancellationToken: cancellationToken);


    #endregion

    #region Private Methods

    /// <summary>
    /// Core method that orchestrates HTTP request sending, response processing, and error handling.
    /// Handles JSON serialization, structured error parsing, and response deserialization.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request body content.</typeparam>
    /// <typeparam name="TResponse">The type to deserialize the successful response into.</typeparam>
    /// <param name="httpMethod">The HTTP method to use (GET, POST, PUT, PATCH, DELETE).</param>
    /// <param name="relativePath">The relative path for the HTTP request (combined with BaseAddress).</param>
    /// <param name="content">Optional request body content to be JSON-serialized.</param>
    /// <param name="addRequestHeaders">Optional callback for adding custom headers to the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the HTTP request.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a tuple with the deserialized response of type <typeparamref name="TResponse"/> and the HTTP response headers.</returns>
    /// <exception cref="HttpStatusCodeException">
    /// Thrown when a non-success status code is received, JSON parsing fails, or response deserialization fails.
    /// Contains structured error information when available from JSON responses.
    /// </exception>
    private async Task<(TResponse response, HttpResponseHeaders headers)> SendRequestAsync<TRequest, TResponse>(
        HttpMethod httpMethod,
        string relativePath,
        TRequest? content = null,
        Action<HttpRequestHeaders>? addRequestHeaders = null,
        CancellationToken cancellationToken = default)
        where TRequest : class
    {
        // Create the HTTP request with standard headers
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = new Uri(relativePath, UriKind.Relative),
            Headers =
            {
                { HttpRequestHeader.Accept.ToString(), MediaTypeNames.Application.Json }
            }
        };

        // Apply any custom headers provided by the caller
        addRequestHeaders?.Invoke(httpRequestMessage.Headers);

        // Serialize request body to JSON if content is provided
        if (content is not null)
        {
            httpRequestMessage.Content = content is HttpContent httpContent
                ? httpContent
                : JsonContent.Create(inputValue: content, options: _options);
        }

        // Execute the HTTP request
        using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, cancellationToken);
        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Handle non-success responses with structured error processing
        if (httpResponseMessage.IsSuccessStatusCode is false)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                throw new HttpStatusCodeException(httpResponseMessage.StatusCode);
            }

            // Attempt to parse structured JSON errors
            try
            {
                var jsonNode = JsonSerializer.Deserialize<JsonNode>(responseContent);
                if (jsonNode is JsonObject jsonObject)
                {
                    throw HttpStatusCodeException.WithJsonObject(
                        httpStatusCode: httpResponseMessage.StatusCode,
                        errors: jsonObject);
                }
            }
            catch (JsonException)
            {
            }

            // Fall back to raw response content for non-JSON errors
            throw new HttpStatusCodeException(
                httpStatusCode: httpResponseMessage.StatusCode,
                message: responseContent);
        }

        // Handle 204 No Content responses (should never have content per HTTP spec)
        if (httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
        {
            return (response: default!, headers: httpResponseMessage.Headers);
        }

        // Handle empty success responses
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return (response: default!, headers: httpResponseMessage.Headers);
        }

        // Deserialize successful JSON responses
        try
        {
            var response = JsonSerializer.Deserialize<TResponse>(responseContent, _options);

            if (response is not null)
            {
                return (response: response, headers: httpResponseMessage.Headers);
            }
        }
        catch (JsonException)
        {
            // JSON deserialization failed or resulted in null - treat as unprocessable entity
        }

        // Handle JSON deserialization failures or null results in success responses
        throw new HttpStatusCodeException(
            httpStatusCode: HttpStatusCode.UnprocessableEntity,
            message: responseContent);
    }

    #endregion
}