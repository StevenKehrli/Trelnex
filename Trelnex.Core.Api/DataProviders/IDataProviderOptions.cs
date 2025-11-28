using FluentValidation;
using Trelnex.Core.Data;

namespace Trelnex.Core.Api.DataProviders;

/// <summary>
/// Defines operations for configuring data provider entity registrations.
/// </summary>
public interface IDataProviderOptions
{
    /// <summary>
    /// Registers an entity type with the data provider configuration.
    /// </summary>
    /// <typeparam name="TItem">The entity type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="typeName">Unique string identifier for the entity type.</param>
    /// <param name="itemValidator">Optional validator for the entity, or null for no validation.</param>
    /// <param name="commandOperations">Allowed CRUD operations for this entity, or null for default operations.</param>
    /// <returns>The same options instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when the type name is invalid or conflicts with existing registrations.</exception>
    IDataProviderOptions Add<TItem>(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null)
        where TItem : BaseItem, new();
}
