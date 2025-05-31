using System.Reflection;
using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Manages dynamic proxies with change tracking and validation.
/// </summary>
/// <typeparam name="TInterface">Interface type exposed to consumers.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Base class for proxy-based command and result classes that provides:
/// - Dynamic proxy management
/// - Method interception for property tracking
/// - Read-only enforcement
/// - Thread-safe property change tracking
/// - Validation capabilities
/// </remarks>
internal abstract class ProxyManager<TInterface, TItem> : IDisposable
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Static Fields

    /// <summary>
    /// Cached property getter methods.
    /// </summary>
    /// <remarks>
    /// Used to determine if a method invocation is a property getter.
    /// </remarks>
    private static readonly PropertyGetters<TItem> _propertyGetters = PropertyGetters<TItem>.Create();

    /// <summary>
    /// Handles property interception for change tracking.
    /// </summary>
    /// <remarks>
    /// Intercepts property operations to detect changes.
    /// </remarks>
    private static readonly TrackProperties<TItem> _trackProperties = TrackProperties<TItem>.Create();

    #endregion

    #region Protected Instance Fields

    /// <summary>
    /// Controls whether modifications to the item are permitted.
    /// </summary>
    /// <remarks>
    /// When true, only property getters are allowed.
    /// </remarks>
    protected bool _isReadOnly;

    /// <summary>
    /// The actual item instance being proxied.
    /// </summary>
    protected TItem _item = null!;

    /// <summary>
    /// The proxy that wraps the underlying item.
    /// </summary>
    protected TInterface _proxy = null!;

    /// <summary>
    /// Thread synchronization primitive.
    /// </summary>
    /// <remarks>
    /// Ensures only one thread can modify the item at a time.
    /// </remarks>
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Validation delegate for the proxied item.
    /// </summary>
    protected ValidateAsyncDelegate<TInterface, TItem> _validateAsyncDelegate = null!;

    #endregion

    #region Private Instance Fields

    /// <summary>
    /// Disposal state flag.
    /// </summary>
    /// <remarks>
    /// Prevents multiple disposal attempts.
    /// </remarks>
    private bool _disposed = false;

    /// <summary>
    /// Collection of tracked property changes.
    /// </summary>
    /// <remarks>
    /// Records property modifications with old and new values.
    /// </remarks>
    private readonly PropertyChanges _propertyChanges = new();

    #endregion

    #region Public Properties

    /// <summary>
    /// Primary access point for consumers to interact with the proxied item.
    /// </summary>
    public TInterface Item => _proxy;

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Runs both system and domain-specific validation rules without modifying the item.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    public async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        return await _validateAsyncDelegate(_item, cancellationToken);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Releases resources based on disposal state.
    /// </summary>
    /// <param name="disposing">
    /// True when called from Dispose(), false when called from finalizer.
    /// </param>
    /// <remarks>
    /// Standard disposal pattern implementation.
    /// </remarks>
    protected virtual void Dispose(
        bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _semaphore.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Core method that handles all proxy invocations.
    /// </summary>
    /// <param name="targetMethod">Intercepted method.</param>
    /// <param name="args">Method arguments.</param>
    /// <returns>Result of the method invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// When modifying a read-only item.
    /// </exception>
    /// <remarks>
    /// Called by the dynamic proxy for all method invocations.
    /// </remarks>
    protected object? OnInvoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        try
        {
            // Acquire a lock to ensure thread safety during method invocation
            _semaphore.Wait();

            // For read-only items, we only allow property getter methods to be called
            if (_isReadOnly && _propertyGetters.IsGetter(targetMethod) is false)
            {
                throw new InvalidOperationException($"The '{typeof(TInterface)}' is read-only.");
            }

            // Delegate the actual method invocation to the _trackProperties helper
            var invokeResult = _trackProperties.Invoke(targetMethod, _item, args);

            // If this invocation modified a property marked with [TrackChange]
            if (invokeResult.IsTracked)
            {
                _propertyChanges.Add(
                    propertyName: invokeResult.PropertyName,
                    oldValue: invokeResult.OldValue,
                    newValue: invokeResult.NewValue);
            }

            // Return the result of the method invocation to the caller
            return invokeResult.Result;
        }
        finally
        {
            // Always release the semaphore to prevent deadlocks
            _semaphore.Release();
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Gets the tracked property changes.
    /// </summary>
    /// <returns>Array of property changes or null if no changes made.</returns>
    /// <remarks>
    /// Returns chronological history of property changes.
    /// </remarks>
    internal PropertyChange[]? GetPropertyChanges()
    {
        return _propertyChanges.ToArray();
    }

    #endregion

    #region Finalizer

    /// <summary>
    /// Finalizer for resource cleanup.
    /// </summary>
    /// <remarks>
    /// Calls Dispose(false) to release unmanaged resources.
    /// </remarks>
    ~ProxyManager()
    {
        Dispose(false);
    }

    #endregion
}
