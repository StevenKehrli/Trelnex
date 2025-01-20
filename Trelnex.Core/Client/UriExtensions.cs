using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Trelnex.Core.Client;

/// <summary>
/// Extension methods for <see cref="Uri"/>.
/// </summary>
public static class UriExtensions
{
    /// <summary>
    /// Append the specified path to the specified <see cref="Uri"/> path.
    /// </summary>
    /// <param name="uri">The specified <see cref="Uri"/>.</param>
    /// <param name="path">The specified path to append to the specified <see cref="Uri"/> path.</param>
    /// <returns>The new <see cref="Uri"/>.</returns>
    public static Uri AppendPath(
        this Uri uri,
        string path)
    {
        // trim the paths
        var absolutePathTrimmed = uri.AbsolutePath.TrimEnd('/');
        var pathTrimmed = path.TrimStart('/');

        return new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: $"{absolutePathTrimmed}/{pathTrimmed}",
            extraValue: uri.Query).Uri;
    }

    /// <summary>
    /// Add the specified key/value to the specified <see cref="Uri"/> query string.
    /// </summary>
    /// <param name="uri">The specified <see cref="Uri"/>.</param>
    /// <param name="key">The name of the query key.</param>
    /// <param name="value">The query value to append to the specified <see cref="Uri"/> query string.</param>
    /// <returns>The new <see cref="Uri"/>.</returns>
    public static Uri AddQueryString(
        this Uri uri,
        string key,
        string value)
    {
        // parse the existing query string
        var kvps = QueryHelpers.ParseQuery(uri.Query);

        // stringValues is a struct
        // so we need to read any existing value
        // add the new key-value pair
        // then set it back in the collection
        kvps.TryGetValue(key, out var stringValues);

        kvps[key] = StringValues.Concat(stringValues, new StringValues(value));

        return new UriBuilder(
            scheme: uri.Scheme,
            host: uri.Host,
            port: uri.Port,
            path: uri.AbsolutePath,
            extraValue: QueryHelpers.AddQueryString(string.Empty, kvps)).Uri;
    }
}
