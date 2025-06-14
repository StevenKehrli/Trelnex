namespace Trelnex.Core.Disposables;

/// <summary>
/// Extension methods for creating DisposableEnumerable instances.
/// </summary>
public static class DisposableEnumerableExtensions
{
    /// <summary>
    /// Converts an array of disposable objects to a DisposableEnumerable.
    /// </summary>
    /// <typeparam name="T">The type of disposable objects.</typeparam>
    /// <param name="disposables">The array of disposables.</param>
    /// <returns>A new DisposableEnumerable instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposables"/> is null.</exception>
    public static IDisposableEnumerable<T> ToDisposableEnumerable<T>(
        this T[] disposables)
        where T : IDisposable
    {
        return DisposableEnumerable<T>.From(disposables);
    }

    /// <summary>
    /// Converts a collection of disposable objects to a DisposableEnumerable.
    /// </summary>
    /// <typeparam name="T">The type of disposable objects.</typeparam>
    /// <param name="disposables">The collection of disposables.</param>
    /// <returns>A new DisposableEnumerable instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposables"/> is null.</exception>
    public static IDisposableEnumerable<T> ToDisposableEnumerable<T>(
        this IEnumerable<T> disposables)
        where T : IDisposable
    {
        return DisposableEnumerable<T>.From(disposables);
    }

    /// <summary>
    /// Converts an IAsyncEnumerable to a DisposableEnumerable by materializing all items.
    /// </summary>
    /// <typeparam name="T">The type of disposable objects.</typeparam>
    /// <param name="asyncEnumerable">The async enumerable of disposables.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A new DisposableEnumerable instance containing all materialized items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="asyncEnumerable"/> is null.</exception>
    public static async Task<IDisposableEnumerable<T>> ToDisposableEnumerableAsync<T>(
        this IAsyncEnumerable<T> asyncEnumerable,
        CancellationToken cancellationToken = default)
        where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(asyncEnumerable);

        var items = await asyncEnumerable.ToArrayAsync(cancellationToken);
        return DisposableEnumerable<T>.From(items);
    }
}
