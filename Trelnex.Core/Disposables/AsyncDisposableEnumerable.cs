namespace Trelnex.Core.Disposables;


/// Represents an async disposable enumerable that manages the disposal of multiple disposable objects.
/// </summary>
/// <typeparam name="T">The type of disposable objects.</typeparam>
public interface IAsyncDisposableEnumerable<out T>
    : IDisposable, IAsyncEnumerable<T>
    where T : IDisposable;

/// <summary>
/// An async disposable enumerable that lazily materializes from an IAsyncEnumerable and manages disposal of items.
/// </summary>
/// <typeparam name="T">The type of disposable objects.</typeparam>
/// <remarks>
/// This class enumerates an IAsyncEnumerable lazily, collecting disposable items as they are enumerated.
/// When disposed, it will dispose all items that have been materialized.
/// If any disposal throws an exception, it will continue disposing remaining items.
/// </remarks>
public sealed class AsyncDisposableEnumerable<T>
    : IAsyncDisposableEnumerable<T>
    where T : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The async enumerable source.
    /// </summary>
    private readonly IAsyncEnumerable<T> _asyncEnumerable;

    /// <summary>
    /// The collection of disposable objects managed by this instance.
    /// </summary>
    private readonly List<T> _disposables = [];

    /// <summary>
    /// Indicates whether this instance has been disposed.
    /// </summary>
    private bool _disposed = false;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableEnumerable{T}"/> class.
    /// </summary>
    /// <param name="asyncEnumerable">The async enumerable source.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="asyncEnumerable"/> is null.</exception>
    public AsyncDisposableEnumerable(
        IAsyncEnumerable<T> asyncEnumerable)
    {
        ArgumentNullException.ThrowIfNull(asyncEnumerable);
        _asyncEnumerable = asyncEnumerable;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates an AsyncDisposableEnumerable from an IAsyncEnumerable.
    /// </summary>
    /// <param name="asyncEnumerable">The async enumerable source.</param>
    /// <returns>A new AsyncDisposableEnumerable instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="asyncEnumerable"/> is null.</exception>
    public static AsyncDisposableEnumerable<T> From(
        IAsyncEnumerable<T> asyncEnumerable)
    {
        ArgumentNullException.ThrowIfNull(asyncEnumerable);
        return new AsyncDisposableEnumerable<T>(asyncEnumerable);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Disposes all materialized disposables.
    /// </summary>
    /// <remarks>
    /// This method will not throw exceptions even if individual disposables fail.
    /// It continues disposing all items to ensure maximum cleanup.
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
    /// Returns an async enumerator that iterates through the collection.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the enumeration.</param>
    /// <returns>An async enumerator for the collection.</returns>
    public async IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in _asyncEnumerable.WithCancellation(cancellationToken))
        {
            _disposables.Add(item);
            yield return item;
        }
    }

    #endregion
}
