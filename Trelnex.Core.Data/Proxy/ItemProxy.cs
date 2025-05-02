using System.Reflection;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides a proxy implementation that intercepts method calls for <typeparamref name="TInterface"/> objects.
/// </summary>
/// <typeparam name="TInterface">The interface type that the proxy implements.</typeparam>
/// <typeparam name="TItem">The concrete implementation type that fulfills the interface contract.</typeparam>
/// <remarks>
/// This proxy uses <see cref="DispatchProxy"/> to intercept and control method invocations,
/// enabling features like change tracking and interception.
/// </remarks>
internal class ItemProxy<TInterface, TItem>
    : DispatchProxy
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// Gets or sets the delegate that handles method invocations on the proxy.
    /// </summary>
    /// <remarks>
    /// This delegate is called whenever any method on the generated proxy type is invoked.
    /// </remarks>
    private Func<MethodInfo?, object?[]?, object?> _onInvoke = null!;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a proxy instance that implements <typeparamref name="TInterface"/>.
    /// </summary>
    /// <param name="onInvoke">The delegate that handles method invocations on the proxy.</param>
    /// <returns>A proxy instance that implements <typeparamref name="TInterface"/>.</returns>
    /// <remarks>
    /// The proxy forwards all method calls to the specified delegate.
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

    /// <summary>
    /// Processes method invocations on the proxy instance.
    /// </summary>
    /// <param name="targetMethod">The method being invoked.</param>
    /// <param name="args">The arguments passed to the method.</param>
    /// <returns>The result of the method invocation.</returns>
    /// <remarks>
    /// This method forwards the invocation to the registered handler delegate.
    /// </remarks>
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        // Forward method invocation to the registered handler delegate
        return _onInvoke(targetMethod, args);
    }

    #endregion
}
