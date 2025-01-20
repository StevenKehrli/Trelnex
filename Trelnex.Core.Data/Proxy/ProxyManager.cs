using System.Reflection;
using FluentValidation.Results;

namespace Trelnex.Core.Data;

internal abstract class ProxyManager<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// The <see cref="PropertyGetters{T}"/> to check if a target method is a property getter.
    /// </summary>
    private static readonly PropertyGetters<TItem> _propertyGetters = PropertyGetters<TItem>.Create();

    /// <summary>
    /// The <see cref="TrackProperties{T}"/> to track changes to properties on the item.
    /// </summary>
    private static readonly TrackProperties<TItem> _trackProperties = TrackProperties<TItem>.Create();

    /// <summary>
    /// Get the item (as a dispatch proxy of the interface type).
    /// </summary>
    public TInterface Item => _proxy;

    /// <summary>
    /// The proxy over the item.
    /// </summary>
    protected TInterface _proxy = null!;

    /// <summary>
    /// The underlying item.
    /// </summary>
    protected TItem _item = null!;

    /// <summary>
    /// A value indicating if the item is read-only.
    /// </summary>
    protected bool _isReadOnly;

    /// <summary>
    /// The action to validate the item.
    /// </summary>
    protected ValidateAsyncDelegate<TInterface, TItem> _validateAsyncDelegate = null!;

    /// <summary>
    /// An exclusive lock to ensure that only one operation that modifies the item is in progress at a time
    /// </summary>
    protected readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly PropertyChanges _propertyChanges = new();

    /// <summary>
    /// The action to validate the item.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The fluent <see cref="ValidationResult"/> item that was saved.</returns>
    public async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        return await _validateAsyncDelegate(_item, cancellationToken);
    }

    /// <summary>
    /// Get the array of <see cref="PropertyChange"/>.
    /// </summary>
    /// <returns>The array of <see cref="PropertyChange"/>.</returns>
    internal PropertyChange[]? GetPropertyChanges()
    {
        return _propertyChanges.ToArray();
    }

    /// <summary>
    /// The method to invoke to dispatch control whenever any method on the generated proxy type is called.
    /// </summary>
    /// <param name="targetMethod">The method the caller invoked.</param>
    /// <param name="args">The arguments the caller passed to the method.</param>
    /// <returns>The item to return to the caller, or null for void methods.</returns>
    /// <exception cref="InvalidOperationException">The proxy is read-only.</exception>
    protected object? OnInvoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        // ensure that only one operation that modifies the item is in progress at a time
        _semaphore.Wait();

        try
        {
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
}
