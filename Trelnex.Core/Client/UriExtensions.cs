using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Trelnex.Core.Client;

/// <summary>
/// Extension methods for manipulating URI objects.
/// </summary>
/// <remarks>
/// Provides utility methods for common URI operations.
/// </remarks>
public static class UriExtensions
{
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
    /// <param name="key">The query parameter name.</param>
    /// <param name="value">The query parameter value.</param>
    /// <returns>A new URI with the added query parameter.</returns>
    /// <example>
    /// <code>
    /// var baseUri = new Uri("https://api.example.com/search?q=test");
    /// var filteredUri = baseUri.AddQueryString("filter", "active");
    /// // Results in: https://api.example.com/search?q=test&amp;filter=active
    /// </code>
    /// </example>
    public static Uri AddQueryString(
        this Uri uri,
        string key,
        string value)
    {
        // Parse the existing query string.
        var kvps = QueryHelpers.ParseQuery(uri.Query);

        // StringValues is a struct, so we need to read any existing value, add the new key-value pair, then set it back in the collection.
        kvps.TryGetValue(key, out var stringValues);

        // Concatenate the existing values with the new value.
        kvps[key] = StringValues.Concat(stringValues, new StringValues(value));

        // Build the new URI with the added query parameter.
        return new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: uri.AbsolutePath,
            extraValue: QueryHelpers.AddQueryString(string.Empty, kvps)).Uri;
    }
}
