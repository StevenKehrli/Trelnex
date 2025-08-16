using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// Configuration builder for data providers.
/// </summary>
/// <remarks>
/// Provides fluent API for configuring data operations and entity type registrations.
/// </remarks>
public interface IDataProviderOptions
{
    /// <summary>
    /// Registers a data provider for an entity type with validation and operation constraints.
    /// </summary>
    /// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
    /// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
    /// <param name="typeName">Unique identifier for the entity type.</param>
    /// <param name="itemValidator">Optional FluentValidation validator for the entity. If null, no validation is performed.</param>
    /// <param name="commandOperations">Permitted CRUD operations for this entity. Defaults to Update operations only if not specified.</param>
    /// <returns>The current options instance to enable method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the type name has invalid format, is null/empty, or conflicts with reserved names.
    /// </exception>
    IDataProviderOptions Add<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();
}
