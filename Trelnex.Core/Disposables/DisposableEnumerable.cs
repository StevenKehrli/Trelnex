namespace Trelnex.Core.Disposables;

/// <summary>
/// A disposable enumerable that manages the disposal of multiple disposable objects.
/// </summary>
/// <typeparam name="T">The type of disposable objects.</typeparam>
/// <remarks>
/// When this enumerable is disposed, it will dispose all contained disposables.
/// If any disposal throws an exception, it will continue disposing the remaining
/// items but will not throw exceptions to avoid masking original exceptions
/// in finally blocks or using statements. Null disposables are not supported.
/// </remarks>
public sealed class DisposableEnumerable<T>
    : IDisposable, IEnumerable<T>
    where T : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The collection of disposable objects managed by this instance.
    /// </summary>
    private readonly List<T> _disposables;

    /// <summary>
    /// Indicates whether this instance has been disposed.
    /// </summary>
    private bool _disposed = false;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableEnumerable{T}"/> class.
    /// </summary>
    /// <param name="disposables">The array of disposables to manage. Null items are not allowed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposables"/> is null.</exception>
    public DisposableEnumerable(
        params T[] disposables)
    {
        ArgumentNullException.ThrowIfNull(disposables);
        _disposables = disposables.ToList();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableEnumerable{T}"/> class.
    /// </summary>
    /// <param name="disposables">The collection of disposables to manage. Null items are not allowed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposables"/> is null.</exception>
    public DisposableEnumerable(
        IEnumerable<T> disposables)
    {
        ArgumentNullException.ThrowIfNull(disposables);
        _disposables = disposables.ToList();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the number of disposable objects in this enumerable.
    /// </summary>
    public int Count => _disposables.Count;

    /// <summary>
    /// Gets the number of disposable objects in this enumerable.
    /// </summary>
    /// <remarks>
    /// This property provides array-like behavior by exposing Length in addition to Count.
    /// </remarks>
    public int Length => _disposables.Count;

    /// <summary>
    /// Gets the disposable object at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the disposable object to get.</param>
    /// <returns>The disposable object at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>.
    /// </exception>
    public T this[int index] => _disposables[index];

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a DisposableEnumerable from an array of disposable objects.
    /// </summary>
    /// <param name="disposables">The array of disposables. Null items are not allowed.</param>
    /// <returns>A new DisposableEnumerable instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposables"/> is null.</exception>
    public static DisposableEnumerable<T> From(
        params T[] disposables)
    {
        ArgumentNullException.ThrowIfNull(disposables);
        return new DisposableEnumerable<T>(disposables);
    }

    /// <summary>
    /// Creates a DisposableEnumerable from a collection of disposable objects.
    /// </summary>
    /// <param name="disposables">The collection of disposables. Null items are not allowed.</param>
    /// <returns>A new DisposableEnumerable instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="disposables"/> is null.</exception>
    public static DisposableEnumerable<T> From(
        IEnumerable<T> disposables)
    {
        ArgumentNullException.ThrowIfNull(disposables);
        return new DisposableEnumerable<T>(disposables);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Disposes all managed disposables.
    /// </summary>
    /// <remarks>
    /// This method will not throw exceptions even if individual disposables fail.
    /// It continues disposing all items to ensure maximum cleanup, following
    /// the principle that Dispose() should be safe to call.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        _disposables.ForEach(disposable =>
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                // Silently continue disposing remaining items
                // Dispose() should not throw exceptions as it's often
                // called from finally blocks or using statements
            }
        });

        _disposed = true;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the disposable objects.
    /// </summary>
    /// <returns>An enumerator for the disposable objects.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _disposables.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the disposable objects.
    /// </summary>
    /// <returns>An enumerator for the disposable objects.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}

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
    public static DisposableEnumerable<T> ToDisposableEnumerable<T>(
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
    public static DisposableEnumerable<T> ToDisposableEnumerable<T>(
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
    public static async Task<DisposableEnumerable<T>> ToDisposableEnumerableAsync<T>(
        this IAsyncEnumerable<T> asyncEnumerable,
        CancellationToken cancellationToken = default)
        where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(asyncEnumerable);

        var items = await asyncEnumerable.ToArrayAsync(cancellationToken);
        return DisposableEnumerable<T>.From(items);
    }
}
