using System.Reflection;

namespace Trelnex.Core.Data;

internal class ItemProxy<TInterface, TItem>
    : DispatchProxy
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// The method to invoke to dispatch control whenever any method on the generated proxy type is called.
    /// </summary>
    private Func<MethodInfo?, object?[]?, object?> _onInvoke = null!;

    /// <summary>
    /// Create a proxy item over a item.
    /// </summary>
    /// <param name="onInvoke">The method to invoke to dispatch control whenever any method on the generated proxy type is called.</param>
    /// <returns>A proxy item as TInterface.</returns>
    public static TInterface Create(
        Func<MethodInfo?, object?[]?, object?> onInvoke)
    {
        // create the dispatch proxy over our item
        var proxy = (Create<TInterface, ItemProxy<TInterface, TItem>>() as ItemProxy<TInterface, TItem>)!;

        // set the onInvoke delegate
        proxy._onInvoke = onInvoke;

        // return the dispatch proxy
        return (proxy as TInterface)!;
    }

    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? args)
    {
        return _onInvoke(targetMethod, args);
    }
}
