namespace Trelnex.Core.Disposables;


/// <summary>
/// Extension methods for creating AsyncDisposableEnumerable instances.
/// </summary>
public static class AsyncDisposableEnumerableExtensions
{
    /// <summary>
    /// Converts an IAsyncEnumerable to an AsyncDisposableEnumerable with automatic disposal management.
    /// </summary>
    /// <typeparam name="T">The type of disposable objects.</typeparam>
    /// <param name="asyncEnumerable">The async enumerable of disposables.</param>
    /// <returns>A new AsyncDisposableEnumerable instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="asyncEnumerable"/> is null.</exception>
    public static AsyncDisposableEnumerable<T> ToAsyncDisposableEnumerable<T>(
        this IAsyncEnumerable<T> asyncEnumerable)
        where T : IDisposable
    {
        return AsyncDisposableEnumerable<T>.From(asyncEnumerable);
    }
}
