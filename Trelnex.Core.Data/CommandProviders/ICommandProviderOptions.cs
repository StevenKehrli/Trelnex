using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines a configuration builder interface for registering and configuring command providers in the data access layer.
/// </summary>
/// <remarks>
/// <para>
/// This interface facilitates registration and configuration of command providers that manage data operations
/// for specific entity types through a fluent API. It allows applications to define which entity types are
/// supported by the data access layer, how they should be validated, and what operations are permitted on each.
/// </para>
/// <para>
/// The options configured through this interface typically feed into a <see cref="ICommandProviderFactory"/>
/// implementation, which uses them to create appropriate command providers for each entity type at runtime.
/// </para>
/// <para>
/// Command providers registered through this interface follow a consistent configuration pattern:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Each provider is associated with a specific interface and implementation type pair
///     </description>
///   </item>
///   <item>
///     <description>
///       Each provider has a type name that identifies its entities in the data store
///     </description>
///   </item>
///   <item>
///     <description>
///       Each provider can have optional validation rules specific to its entity type
///     </description>
///   </item>
///   <item>
///     <description>
///       Each provider can be configured with specific operation permissions (update/delete)
///     </description>
///   </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Configure command providers during application startup:
/// services.AddCommandProviders(options =>
/// {
///     options
///         .Add&lt;IUser, User&gt;(
///             typeName: "user",
///             validator: new UserValidator(),
///             commandOperations: CommandOperations.All)
///         .Add&lt;IProduct, Product&gt;(
///             typeName: "product",
///             validator: new ProductValidator())
///         .Add&lt;IAuditLog, AuditLog&gt;(
///             typeName: "audit-log",
///             commandOperations: CommandOperations.None); // Read-only
/// });
/// </code>
/// </example>
/// <seealso cref="ICommandProviderFactory"/>
/// <seealso cref="CommandProvider{TInterface, TItem}"/>
/// <seealso cref="CommandOperations"/>
public interface ICommandProviderOptions
{
    #region Public Methods

    /// <summary>
    /// Registers a <see cref="ICommandProvider{TInterface}"/> for the specified interface and item type.
    /// </summary>
    /// <typeparam name="TInterface">The interface type that defines the contract for the item.</typeparam>
    /// <typeparam name="TItem">The concrete implementation type that implements the specified interface.</typeparam>
    /// <param name="typeName">
    /// The type name identifier used in the data store for this entity type.
    /// This value is stored in the <see cref="BaseItem.TypeName"/> property of each item.
    /// </param>
    /// <param name="validator">
    /// Optional FluentValidation validator for domain-specific validation rules.
    /// Pass <see langword="null"/> to skip custom validation (basic property validation still applies).
    /// </param>
    /// <param name="commandOperations">
    /// The operations permitted on this entity type (update and/or delete).
    /// By default, only updates are allowed (<see cref="CommandOperations.Update"/>).
    /// </param>
    /// <returns>The <see cref="ICommandProviderOptions"/> instance for fluent method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers a command provider for the specified entity type and configures
    /// its validation and operation settings. Use this to define which operations are permitted
    /// on specific entity types and how they should be validated.
    /// </para>
    /// <para>
    /// The type name must follow specific naming rules (lowercase letters and hyphens,
    /// starting and ending with a letter) and cannot be a reserved system type name.
    /// It serves as a discriminator in the data store, allowing different entity types
    /// to be stored together but queried separately.
    /// </para>
    /// <para>
    /// The validator parameter allows for domain-specific validation rules to be applied
    /// to entities before they are persisted, ensuring data integrity beyond basic property validation.
    /// </para>
    /// <para>
    /// The commandOperations parameter controls which modification operations are permitted.
    /// Create operations are always allowed, but update and delete can be selectively enabled.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the provided type name does not follow naming conventions or is a reserved name.
    /// </exception>
    ICommandProviderOptions Add<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new();

    #endregion
}
