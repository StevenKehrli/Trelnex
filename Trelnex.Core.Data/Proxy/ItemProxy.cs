using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Proxy that intercepts method calls for <typeparamref name="TInterface"/> objects.
/// </summary>
/// <typeparam name="TInterface">Interface type implemented by proxy.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Extends <see cref="DispatchProxy"/> to intercept method calls.
/// </remarks>
internal class ItemProxy<TInterface, TItem>
    : DispatchProxy
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// Delegate that handles method invocations.
    /// </summary>
    /// <remarks>
    /// Called for all method invocations on the proxy instance.
    /// </remarks>
    private Func<MethodInfo?, object?[]?, object?> _onInvoke = null!;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a proxy instance implementing <typeparamref name="TInterface"/>.
    /// </summary>
    /// <param name="onInvoke">Delegate for handling method invocations.</param>
    /// <returns>Proxy instance implementing <typeparamref name="TInterface"/>.</returns>
    /// <remarks>
    /// Configures a proxy to forward all calls to the provided delegate.
    /// </remarks>
    public static TInterface Create(
        Func<MethodInfo?, object?[]?, object?> onInvoke)
    {
        // Create a new proxy instance using DispatchProxy.Create<TInterface, ItemProxy<>>
        var proxy = (Create<TInterface, ItemProxy<TInterface, TItem>>() as ItemProxy<TInterface, TItem>)!;

        // Register the invocation handler that will be called for all method calls
        proxy._onInvoke = onInvoke;

        // Return the proxy instance cast to the interface type
        return (proxy as TInterface)!;
    }

    #endregion

    #region Protected Methods

    /// <inheritdoc/>
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        // Forward method invocation to the registered handler delegate
        return _onInvoke(targetMethod, args);
    }

    #endregion
}
