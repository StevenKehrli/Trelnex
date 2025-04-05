using FluentValidation;

namespace Trelnex.Core.Data;

public interface ICommandProviderOptions
{
    /// <summary>
    /// Injects a <see cref="ICommandProvider{TInterface}"/> for the specified interface and item type.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type.</typeparam>
    /// <typeparam name="TItem">The specified item type that implements the specified interface type.</typeparam>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed. By default, update is allowed; delete is not allowed.</param>
    /// <returns>The <see cref="ICommandProviderOptions"/>.</returns>
    ICommandProviderOptions Add<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();
}
