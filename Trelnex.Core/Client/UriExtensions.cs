using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Trelnex.Core.Client;

/// <summary>
/// Extension methods for manipulating URI objects.
/// </summary>
/// <remarks>
/// Provides utility methods for common URI operations.
/// </remarks>
public static class UriExtensions
{
    #region Public Static Methods

    /// <summary>
    /// Appends a path segment to a URI.
    /// </summary>
    /// <param name="uri">The base URI to extend.</param>
    /// <param name="path">The path segment to append.</param>
    /// <returns>A new URI with the combined path.</returns>
    /// <example>
    /// <code>
    /// var baseUri = new Uri("https://api.example.com/v1");
    /// var resourceUri = baseUri.AppendPath("users");
    /// // Results in: https://api.example.com/v1/users
    /// </code>
    /// </example>
    public static Uri AppendPath(
        this Uri uri,
        string path)
    {
        // Trim the paths to ensure there is exactly one slash between the original path and the appended path.
        var absolutePathTrimmed = uri.AbsolutePath.TrimEnd('/');
        var pathTrimmed = path.TrimStart('/');

        // Build the new URI with the combined path.
        return new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: $"{absolutePathTrimmed}/{pathTrimmed}",
            extraValue: uri.Query).Uri;
    }

    /// <summary>
    /// Adds or appends a query string parameter to a URI.
    /// </summary>
    /// <param name="uri">The base URI to extend.</param>
    /// <param name="parameter">A tuple containing the query parameter name and value.</param>
    /// <returns>A new URI with the added query parameter.</returns>
    /// <example>
    /// <code>
    /// var baseUri = new Uri("https://api.example.com/search?q=test");
    /// var filteredUri = baseUri.AddQueryString(("filter", "active"));
    /// // Results in: https://api.example.com/search?q=test&amp;filter=active
    /// </code>
    /// </example>
    public static Uri AddQueryString(
        this Uri uri,
        (string key, string value) parameter)
    {
        // Parse the existing query string.
        var kvps = QueryHelpers.ParseQuery(uri.Query);

        // StringValues is a struct, so we need to read any existing value, add the new key-value pair, then set it back in the collection.
        kvps.TryGetValue(parameter.key, out var stringValues);

        // Concatenate the existing values with the new value.
        kvps[parameter.key] = StringValues.Concat(stringValues, new StringValues(parameter.value));

        // Build the new URI with the added query parameter.
        return new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: uri.AbsolutePath,
            extraValue: QueryHelpers.AddQueryString(string.Empty, kvps)).Uri;
    }

    /// <summary>
    /// Adds or appends multiple query string parameters to a URI.
    /// </summary>
    /// <param name="uri">The base URI to extend.</param>
    /// <param name="parameters">An array of tuples containing query parameter names and values.</param>
    /// <returns>A new URI with the added query parameters.</returns>
    /// <example>
    /// <code>
    /// var baseUri = new Uri("https://api.example.com/search");
    /// var parameters = new[] { ("q", "test"), ("filter", "active"), ("page", "1") };
    /// var filteredUri = baseUri.AddQueryString(parameters);
    /// // Results in: https://api.example.com/search?q=test&amp;filter=active&amp;page=1
    /// </code>
    /// </example>
    public static Uri AddQueryString(
        this Uri uri,
        params (string key, string value)[] parameters)
    {
        // If no parameters are provided, return the original URI
        if (parameters.Length == 0) return uri;

        // Parse the existing query string.
        var kvps = QueryHelpers.ParseQuery(uri.Query);

        // Add each parameter to the query string collection.
        foreach (var parameter in parameters)
        {
            // StringValues is a struct, so we need to read any existing value, add the new key-value pair, then set it back in the collection.
            kvps.TryGetValue(parameter.key, out var stringValues);

            // Concatenate the existing values with the new value.
            kvps[parameter.key] = StringValues.Concat(stringValues, new StringValues(parameter.value));
        }

        // Build the new URI with the added query parameters.
        return new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: uri.AbsolutePath,
            extraValue: QueryHelpers.AddQueryString(string.Empty, kvps)).Uri;
    }

    /// <summary>
    /// Adds or appends query string parameters to a URI by serializing an object.
    /// </summary>
    /// <typeparam name="TRequest">The type of the object to serialize as query parameters.</typeparam>
    /// <param name="uri">The base URI to extend.</param>
    /// <param name="content">The object to serialize as query string parameters.</param>
    /// <param name="options">Optional JSON serialization options.</param>
    /// <returns>A new URI with the added query parameters, or the original URI if content is null.</returns>
    /// <example>
    /// <code>
    /// var baseUri = new Uri("https://api.example.com/search");
    /// var searchParams = new { q = "test", filter = "active", page = 1 };
    /// var filteredUri = baseUri.AddQueryString(searchParams);
    /// // Results in: https://api.example.com/search?q=test&amp;filter=active&amp;page=1
    /// </code>
    /// </example>
    public static Uri AddQueryString<TRequest>(
        this Uri uri,
        TRequest? content,
        JsonSerializerOptions? options = null)
    {
        // If no content is provided, return the original URI
        if (content is null) return uri;

        // Serialize the content directly to JsonNode
        var jsonNode = JsonSerializer.SerializeToNode(content, options);

        // If the jsonNode is not an object, throw an exception
        if (jsonNode is not JsonObject)
        {
            throw new ArgumentException("Content must serialize to a JSON object with properties.", nameof(content));
        }

        // Convert JsonObject properties to array of tuples, filtering out null values
        var parameters = jsonNode
            .AsObject()
            .Select(kvp => Convert(kvp.Key, kvp.Value))
            .Where(result => result.HasValue)
            .ToArray();

        // Add query parameters to the URI using the existing method
        return uri.AddQueryString(parameters);
    }

    /// <summary>
    /// Validates that a JsonNode value is suitable for query string parameters and converts it to a tuple.
    /// </summary>
    /// <param name="propertyName">The name of the property for error reporting.</param>
    /// <param name="value">The JsonNode value to validate and convert.</param>
    /// <returns>A tuple containing the property name and its string representation, or null if the value should be skipped.</returns>
    /// <exception cref="ArgumentException">Thrown when the value type is not supported for query strings.</exception>
    private static (string key, string value)? Convert(
        string propertyName,
        JsonNode? value)
    {
        return value?.GetValueKind() switch
        {
            null => null,
            JsonValueKind.Null => null,
            JsonValueKind.String => (key: propertyName, value: value.GetValue<string>()),
            JsonValueKind.Number => (key: propertyName, value: value.ToString()),
            JsonValueKind.True => (key: propertyName, value: "true"),
            JsonValueKind.False => (key: propertyName, value: "false"),

            _ => throw new ArgumentException($"Property '{propertyName}' has unsupported type '{value.GetValueKind()}' for query string parameters. Only primitive values are supported.")
        };
    }

    #endregion
}
