using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines configuration options for command providers in the data access layer.
/// </summary>
/// <remarks>
/// This interface facilitates registration and configuration of command providers
/// that manage data operations for specific entity types. It provides a fluent API
/// for setting up command providers with appropriate validation and operation permissions.
/// </remarks>
public interface ICommandProviderOptions
{
    #region Public Methods

    /// <summary>
    /// Injects a <see cref="ICommandProvider{TInterface}"/> for the specified interface and item type.
    /// </summary>
    /// <typeparam name="TInterface">The specified interface type that defines the contract for the item.</typeparam>
    /// <typeparam name="TItem">The specified concrete item type that implements the specified interface.</typeparam>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <param name="validator">The fluent validator for the item. Pass <see langword="null"/> to skip validation.</param>
    /// <param name="commandOperations">The value indicating if update and delete commands are allowed.
    /// By default, update is allowed; delete is not allowed.</param>
    /// <returns>The <see cref="ICommandProviderOptions"/> instance for method chaining.</returns>
    /// <remarks>
    /// This method registers a command provider for the specified item type and configures
    /// its validation and operation settings. Use this to define which operations are permitted
    /// on specific entity types and how they should be validated.
    /// </remarks>
    ICommandProviderOptions Add<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();

    #endregion
}
