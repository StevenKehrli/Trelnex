using System.Reflection;
using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides base functionality for managing proxied items with change tracking and validation capabilities.
/// </summary>
/// <typeparam name="TInterface">The interface type that the proxy implements.</typeparam>
/// <typeparam name="TItem">The concrete implementation type that fulfills the interface contract.</typeparam>
/// <remarks>
/// This abstract class serves as the foundation for proxy-based data access patterns,
/// enabling features like property change tracking, validation, and read-only protection.
/// </remarks>
internal abstract class ProxyManager<TInterface, TItem> : IDisposable
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Static Fields

    /// <summary>
    /// Gets a cached collection of property getter methods for the <typeparamref name="TItem"/> type.
    /// </summary>
    /// <remarks>
    /// Used to determine if a method being invoked is a property getter, which affects read-only behavior.
    /// </remarks>
    private static readonly PropertyGetters<TItem> _propertyGetters = PropertyGetters<TItem>.Create();

    /// <summary>
    /// Gets a handler that intercepts property operations to enable change tracking for <typeparamref name="TItem"/> instances.
    /// </summary>
    private static readonly TrackProperties<TItem> _trackProperties = TrackProperties<TItem>.Create();

    #endregion

    #region Protected Instance Fields

    /// <summary>
    /// Gets or sets a value indicating whether modifications to the item are permitted.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the item is read-only; otherwise, <see langword="false"/>.
    /// </value>
    protected bool _isReadOnly;
    
    /// <summary>
    /// Gets or sets the actual item instance being proxied.
    /// </summary>
    /// <remarks>
    /// This is the concrete implementation that fulfills the operations defined by <typeparamref name="TInterface"/>.
    /// </remarks>
    protected TItem _item = null!;
    
    /// <summary>
    /// Gets or sets the proxy implementation of the item.
    /// </summary>
    /// <remarks>
    /// This is the interface wrapper that intercepts all method calls to the underlying item.
    /// </remarks>
    protected TInterface _proxy = null!;
    
    /// <summary>
    /// Gets a synchronization primitive that ensures thread-safe access to the proxied item.
    /// </summary>
    /// <remarks>
    /// This semaphore prevents concurrent modifications to the item, ensuring data integrity.
    /// </remarks>
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Gets or sets the delegate that performs validation on the proxied item.
    /// </summary>
    /// <remarks>
    /// This delegate is invoked by <see cref="ValidateAsync"/> to validate the item state.
    /// </remarks>
    protected ValidateAsyncDelegate<TInterface, TItem> _validateAsyncDelegate = null!;

    #endregion

    #region Private Instance Fields

    /// <summary>
    /// Tracks whether this instance has been disposed.
    /// </summary>
    private bool _disposed = false;
    
    /// <summary>
    /// Gets the collection that tracks all property changes made to the proxied item.
    /// </summary>
    /// <remarks>
    /// This collection maintains the history of property modifications for later retrieval.
    /// </remarks>
    private readonly PropertyChanges _propertyChanges = new();

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the proxied item instance that implements <typeparamref name="TInterface"/>.
    /// </summary>
    /// <value>A proxy-wrapped instance of the underlying item.</value>
    public TInterface Item => _proxy;

    #endregion

    #region Public Methods

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="ProxyManager{TInterface, TItem}"/> class.
    /// </summary>
    /// <remarks>
    /// This method follows the standard .NET dispose pattern.
    /// <para>
    /// It calls <see cref="Dispose(bool)"/> with <see langword="true"/> to release managed and unmanaged resources,
    /// and suppresses finalization to prevent redundant cleanup.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Validates the current state of the proxied item.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValidationResult"/> containing the validation outcome.</returns>
    /// <remarks>
    /// This method delegates to the configured validation delegate to perform business rule validation.
    /// </remarks>
    public async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        return await _validateAsyncDelegate(_item, cancellationToken);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Retrieves the collection of tracked property changes.
    /// </summary>
    /// <returns>
    /// An ordered array of <see cref="PropertyChange"/> objects representing changes made to the item,
    /// or <see langword="null"/> if no changes exist.
    /// </returns>
    internal PropertyChange[]? GetPropertyChanges()
    {
        return _propertyChanges.ToArray();
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Releases resources used by the <see cref="ProxyManager{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; 
    /// <see langword="false"/> to release only unmanaged resources.
    /// </param>
    /// <remarks>
    /// When <paramref name="disposing"/> is <see langword="true"/>, this method releases the semaphore
    /// and any other managed resources. This method is called by both the <see cref="Dispose()"/> method 
    /// and the finalizer.
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
    /// Handles method invocations on the proxy object.
    /// </summary>
    /// <param name="targetMethod">The intercepted method being called.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a read-only item with a non-getter method.
    /// </exception>
    /// <remarks>
    /// This method enforces read-only restrictions, tracks property changes, and ensures thread safety
    /// during method invocations on the proxied item.
    /// </remarks>
    protected object? OnInvoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        try
        {
            // ensure that only one operation that modifies the item is in progress at a time
            _semaphore.Wait();

            // if the item is read only, throw an exception
            if (_isReadOnly && _propertyGetters.IsGetter(targetMethod) is false)
            {
                throw new InvalidOperationException($"The '{typeof(TInterface)}' is read-only.");
            }

            // invoke the target method and capture the change
            var invokeResult = _trackProperties.Invoke(targetMethod, _item, args);

            if (invokeResult.IsTracked)
            {
                _propertyChanges.Add(
                    propertyName: invokeResult.PropertyName,
                    oldValue: invokeResult.OldValue,
                    newValue: invokeResult.NewValue);
            }

            return invokeResult.Result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    #endregion

    #region Finalizer

    /// <summary>
    /// Allows the <see cref="ProxyManager{TInterface, TItem}"/> instance to attempt to free resources
    /// even when Dispose is not explicitly called.
    /// </summary>
    /// <remarks>
    /// The finalizer calls the <see cref="Dispose(bool)"/> method with <see langword="false"/>,
    /// indicating that only unmanaged resources should be released.
    /// </remarks>
    ~ProxyManager()
    {
        Dispose(false);
    }

    #endregion
}
