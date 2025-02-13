using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Trelnex.Core.Client;

/// <summary>
/// Initializes a new instance of <see cref="BaseClient"/> with the specified <see cref="HttpClient"/>.
/// </summary>
/// <param name="httpClient">The specified <see cref="HttpClient"/> instance.</param>
public abstract class BaseClient(
    HttpClient httpClient)
{
    /// <summary>
    /// The options to be used with <see cref="JsonSerializer"/> to serialize the request content and deserialize the response content.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Gets the base <see cref="Uri"/> to build the request <see cref="Uri"/>.
    /// </summary>
    protected Uri BaseAddress => httpClient.BaseAddress ?? throw new ConfigurationErrorsException("BaseAddress is not set.");

    /// <summary>
    /// Asks the service to delete the specified resource.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="uri">The specified <see cref="Uri"/> to send the request.</param>
    /// <param name="addHeaders">An optional delegate to add headers to <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="errorHandler">An optional delegate to handle a response error.</param>
    /// <returns>The <see cref="TResponse"/>.</returns>
    protected async Task<TResponse> Delete<TResponse>(
        Uri uri,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
    {
        return await SendRequest<object, TResponse>(
            httpMethod: HttpMethod.Delete,
            uri: uri,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Asks the service for the specified resource.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="uri">The specified <see cref="Uri"/> to send the request.</param>
    /// <param name="addHeaders">An optional delegate to add headers to <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="errorHandler">An optional delegate to handle a response error.</param>
    /// <returns>The <see cref="TResponse"/>.</returns>
    protected async Task<TResponse> Get<TResponse>(
        Uri uri,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
    {
        return await SendRequest<object, TResponse>(
            httpMethod: HttpMethod.Get,
            uri: uri,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Asks the service to apply partial modifications to the specified resource.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="uri">The specified <see cref="Uri"/> to send the request.</param>
    /// <param name="content">The request body specifying the partial modifications.</param>
    /// <param name="addHeaders">An optional delegate to add headers to <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="errorHandler">An optional delegate to handle a response error.</param>
    /// <returns>The <see cref="TResponse"/>.</returns>
    protected async Task<TResponse> Patch<TRequest, TResponse>(
        Uri uri,
        TRequest? content,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        return await SendRequest<TRequest, TResponse>(
            httpMethod: HttpMethod.Patch,
            uri: uri,
            content: content,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Asks the service to create the specified resource.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="uri">The specified <see cref="Uri"/> to send the request.</param>
    /// <param name="content">The request body specifying the resource.</param>
    /// <param name="addHeaders">An optional delegate to add headers to <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="errorHandler">An optional delegate to handle a response error.</param>
    /// <returns>The <see cref="TResponse"/>.</returns>
    protected async Task<TResponse> Post<TRequest, TResponse>(
        Uri uri,
        TRequest? content,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        return await SendRequest<TRequest, TResponse>(
            httpMethod: HttpMethod.Post,
            uri: uri,
            content: content,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Asks the service to create or replace the specified resource.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="uri">The specified <see cref="Uri"/> to send the request.</param>
    /// <param name="content">The request body specifying the resource.</param>
    /// <param name="addHeaders">An optional delegate to add headers to <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="errorHandler">An optional delegate to handle a response error.</param>
    /// <returns>The <see cref="TResponse"/>.</returns>
    protected async Task<TResponse> Put<TRequest, TResponse>(
        Uri uri,
        TRequest? content,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        return await SendRequest<TRequest, TResponse>(
            httpMethod: HttpMethod.Put,
            uri: uri,
            content: content,
            addHeaders: addHeaders,
            errorHandler: errorHandler);
    }

    /// <summary>
    /// Sends the request to the service.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="httpMethod">Represents the HTTP protocol method.</param>
    /// <param name="uri">The specified <see cref="Uri"/> to send the request.</param>
    /// <param name="content">The request body specifying the resource.</param>
    /// <param name="addHeaders">An optional delegate to add headers to <see cref="HttpRequestHeaders"/>.</param>
    /// <param name="errorHandler">An optional delegate to handle a response error.</param>
    /// <returns>The <see cref="TResponse"/>.</returns>
    private async Task<TResponse> SendRequest<TRequest, TResponse>(
        HttpMethod httpMethod,
        Uri uri,
        TRequest? content = null,
        Action<HttpRequestHeaders>? addHeaders = null,
        Func<JsonNode, string?>? errorHandler = null)
        where TRequest : class
    {
        var clientName = GetType().FullName;

        // build our request message
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = httpMethod,
            RequestUri = uri,
            Headers =
            {
                { HttpRequestHeader.Accept.ToString(), MediaTypeNames.Application.Json }
            }
        };

        // add any additional headers
        addHeaders?.Invoke(httpRequestMessage.Headers);

        if (content is not null)
        {
            // serialize the content to the request body
            _ = (content is HttpContent httpContent)
                ? httpRequestMessage.Content = httpContent
                : httpRequestMessage.Content = JsonContent.Create(
                    inputValue: content,
                    options: _options);
        }

        using var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

        // read the response content as string
        var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

        if (httpResponseMessage.IsSuccessStatusCode is false)
        {
            // format an error message and default to the status code
            var message = httpResponseMessage.StatusCode.ToReason();

            // if there is no response content, there is nothing further we can do
            // if there is no error handler, there is nothing further we can do
            if (string.IsNullOrWhiteSpace(responseContent) || errorHandler is null)
            {
                throw new HttpStatusCodeException(
                    httpStatusCode: httpResponseMessage.StatusCode,
                    message: $"{clientName}: {message}");
            }

            try
            {
                // try to override the message with the error handler
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
                throw new HttpStatusCodeException(
                    httpStatusCode: httpResponseMessage.StatusCode,
                    message: $"{clientName}: {message}");
            }
        }

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return default!;
        }

        try
        {
            // try to deserialize the response content (string) as TResponse
            var response = JsonSerializer.Deserialize<TResponse>(responseContent, _options);

            if (response is not null) return response;

            throw new HttpStatusCodeException(
                httpStatusCode: HttpStatusCode.UnprocessableContent,
                message: responseContent);
        }
        catch
        {
            throw new HttpStatusCodeException(
                httpStatusCode: HttpStatusCode.UnprocessableContent,
                message: responseContent);
        }
    }
}
