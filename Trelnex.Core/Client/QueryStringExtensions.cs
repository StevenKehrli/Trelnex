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
    #region Private Static Fields

    /// <summary>
    /// Fake base URI used for converting relative URIs to absolute URIs for UriBuilder operations.
    /// </summary>
    private static readonly Uri s_fakeSchemeHostPort = new("http://localhost:8080/");

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Adds or appends multiple query string parameters to a relative path string.
    /// </summary>
    /// <param name="relativePath">The relative path string to extend with query parameters.</param>
    /// <param name="parameters">An array of tuples containing query parameter names and values.</param>
    /// <returns>A new relative path string with the added query parameters.</returns>
    /// <remarks>
    /// This method uses a fake absolute URI workaround to leverage UriBuilder for proper query string handling,
    /// since UriBuilder cannot work directly with relative URIs. The fake URI is used internally and stripped
    /// from the final result, returning only the relative path with query parameters.
    /// </remarks>
    /// <example>
    /// <code>
    /// var relativePath = "api/users";
    /// var parameters = new[] { ("filter", "active"), ("page", "1") };
    /// var result = relativePath.AddQueryString(parameters);
    /// // Results in: "api/users?filter=active&amp;page=1"
    /// </code>
    /// </example>
    public static string AddQueryString(
        this string relativePath,
        params (string key, string value)[] parameters)
    {
        // If no parameters are provided, return the original relative path unchanged
        if (parameters.Length == 0) return relativePath;

        // Convert relative path to fake absolute URI for both query parsing and UriBuilder operations
        var fakeUri = new Uri(s_fakeSchemeHostPort, relativePath);

        // Parse any existing query string parameters from the relative path
        var kvps = QueryHelpers.ParseQuery(fakeUri.Query);

        // Add each new parameter to the existing query string collection
        foreach (var (key, value) in parameters)
        {
            // StringValues is a struct, so we need to read any existing value, add the new key-value pair, then set it back in the collection
            kvps.TryGetValue(key, out var stringValues);

            // Concatenate the existing values with the new value (supports multiple values per key)
            kvps[key] = StringValues.Concat(stringValues, new StringValues(value));
        }

        // Build the final URI with the combined query parameters using UriBuilder
        var uriBuilder = new UriBuilder(fakeUri)
        {
            // Replace the query string with our combined parameters using QueryHelpers for proper encoding
            Query = QueryHelpers.AddQueryString(string.Empty, kvps)
        };

        // Extract only the relative portion (path + query) from the absolute URI, discarding the fake scheme/host/port
        return uriBuilder.Uri.PathAndQuery;
    }

    #endregion
}