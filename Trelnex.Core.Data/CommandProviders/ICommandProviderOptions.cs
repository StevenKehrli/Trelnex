using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// Configuration builder for command providers.
/// </summary>
/// <remarks>
/// Provides fluent API for configuring data operations.
/// </remarks>
public interface ICommandProviderOptions
{
    /// <summary>
    /// Registers command provider for an entity type.
    /// </summary>
    /// <typeparam name="TInterface">Interface type defining the contract.</typeparam>
    /// <typeparam name="TItem">Concrete implementation type.</typeparam>
    /// <param name="typeName">Type name identifier.</param>
    /// <param name="validator">Optional validator.</param>
    /// <param name="commandOperations">Permitted operations (default: Update only).</param>
    /// <returns>Options instance for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// When type name has invalid format or is reserved.
    /// </exception>
    ICommandProviderOptions Add<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();
}
