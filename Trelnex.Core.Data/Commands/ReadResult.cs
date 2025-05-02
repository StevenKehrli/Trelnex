using FluentValidation.Results;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines a contract for read-only access to an item retrieved from a data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="IReadResult{TInterface}"/> interface provides a standardized way to access items
/// retrieved from data stores while enforcing read-only access and offering validation capabilities.
/// It acts as a wrapper around the retrieved item, controlling access and ensuring data integrity.
/// </para>
/// <para>
/// Key characteristics of this interface include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Read-only access to the underlying item through the <see cref="Item"/> property
///     </description>
///   </item>
///   <item>
///     <description>
///       Validation capabilities via the <see cref="ValidateAsync"/> method to verify item state
///     </description>
///   </item>
///   <item>
///     <description>
///       Type safety through generic constraints requiring items to implement <see cref="IBaseItem"/>
///     </description>
///   </item>
/// </list>
/// <para>
/// This interface is returned by various operations in the data access layer, including:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>Direct read operations (<c>ReadAsync</c>)</description>
///   </item>
///   <item>
///     <description>Save operations after creating, updating, or deleting items</description>
///   </item>
///   <item>
///     <description>Batch operations (<see cref="IBatchResult{TInterface}.ReadResult"/>)</description>
///   </item>
/// </list>
/// <para>
/// Implementations of this interface typically use dynamic proxies to intercept access attempts
/// and enforce read-only behavior while allowing validation without modification.
/// </para>
/// </remarks>
/// <seealso cref="IBaseItem"/>
/// <seealso cref="IBatchResult{TInterface}"/>
/// <seealso cref="ISaveCommand{TInterface}"/>
public interface IReadResult<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Gets the item retrieved from the data store with read-only access.
    /// </summary>
    /// <value>
    /// A strongly-typed instance of <typeparamref name="TInterface"/> representing the retrieved item.
    /// All property access is read-only and any attempt to modify properties will throw an exception.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property provides access to the underlying item that was retrieved from the data store.
    /// The item is wrapped in a dynamic proxy that intercepts all method calls, enforcing read-only
    /// access to maintain data integrity.
    /// </para>
    /// <para>
    /// Attempting to modify any property will result in an <see cref="InvalidOperationException"/> with
    /// a message indicating that the item is read-only.
    /// </para>
    /// <para>
    /// Property access is allowed, enabling read operations like:
    /// </para>
    /// <code>
    /// var id = readResult.Item.Id;
    /// var typeName = readResult.Item.TypeName;
    /// var createdDate = readResult.Item.CreatedDate;
    /// </code>
    /// <para>
    /// To modify an item retrieved through an <see cref="IReadResult{TInterface}"/>, you must first
    /// convert it to an <see cref="ISaveCommand{TInterface}"/> using appropriate methods provided
    /// by the command provider.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an attempt is made to modify any property of the item.
    /// </exception>
    TInterface Item { get; }

    /// <summary>
    /// Validates the current state of the retrieved item against configured validation rules.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task representing the asynchronous validation operation. The task result contains
    /// a <see cref="ValidationResult"/> indicating whether the item is valid and listing
    /// any validation errors that were found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method executes both system-defined and domain-specific validation rules against
    /// the item without modifying it. The validation rules typically include:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Base validation rules for <see cref="IBaseItem"/> properties like Id, PartitionKey, etc.</description>
    ///   </item>
    ///   <item>
    ///     <description>Domain-specific validation rules defined for the concrete item type</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The validation is performed asynchronously to accommodate validation rules that might
    /// require external resource access or time-consuming operations.
    /// </para>
    /// <para>
    /// Common usage patterns include:
    /// </para>
    /// <code>
    /// // Simple validation
    /// var validationResult = await readResult.ValidateAsync(cancellationToken);
    /// bool isValid = validationResult.IsValid;
    ///
    /// // Validation with error handling
    /// var validationResult = await readResult.ValidateAsync(cancellationToken);
    /// if (!validationResult.IsValid)
    /// {
    ///     foreach (var error in validationResult.Errors)
    ///     {
    ///         Console.WriteLine($"Error: {error.ErrorMessage}");
    ///     }
    /// }
    /// </code>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the <paramref name="cancellationToken"/>.
    /// </exception>
    /// <seealso cref="FluentValidation.Results.ValidationResult"/>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements the read-only access and validation logic for items retrieved from the data store.
/// </summary>
/// <typeparam name="TInterface">The interface type representing the items in the data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items.</typeparam>
/// <remarks>
/// <para>
/// This class provides a concrete implementation of <see cref="IReadResult{TInterface}"/> that
/// wraps retrieved items in a read-only proxy to prevent modification. It leverages the
/// <see cref="ProxyManager{TInterface, TItem}"/> base class to handle the dynamic proxy creation
/// and method interception.
/// </para>
/// <para>
/// The class enforces these key behaviors:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       Read-only access to all properties by setting the <c>_isReadOnly</c> flag to <see langword="true"/>
///     </description>
///   </item>
///   <item>
///     <description>
///       Property value access is permitted, but property value modification is prohibited
///     </description>
///   </item>
///   <item>
///     <description>
///       Validation capabilities that execute both system and domain-specific validation rules
///     </description>
///   </item>
/// </list>
/// <para>
/// Instances of this class are created using the static <see cref="Create"/> factory method, which
/// configures the proxy and ensures proper initialization.
/// </para>
/// <para>
/// This implementation isolates consumers from the concrete implementation details of items,
/// providing a consistent interface-based programming model regardless of the underlying
/// data storage mechanism.
/// </para>
/// </remarks>
/// <seealso cref="IReadResult{TInterface}"/>
/// <seealso cref="ProxyManager{TInterface, TItem}"/>
internal class ReadResult<TInterface, TItem>
    : ProxyManager<TInterface, TItem>, IReadResult<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Public Methods

    /// <summary>
    /// Creates a new instance of <see cref="ReadResult{TInterface, TItem}"/> that wraps the provided item with read-only access.
    /// </summary>
    /// <param name="item">The concrete item instance to be wrapped in a read-only proxy.</param>
    /// <param name="validateAsyncDelegate">The delegate used to validate the item against business rules.</param>
    /// <returns>A fully configured <see cref="ReadResult{TInterface, TItem}"/> instance that implements <see cref="IReadResult{TInterface}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This factory method creates and configures a <see cref="ReadResult{TInterface, TItem}"/> instance
    /// with all necessary components for proper operation. It uses the following steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Creates a new <see cref="ReadResult{TInterface, TItem}"/> instance</description>
    ///   </item>
    ///   <item>
    ///     <description>Sets the underlying item reference and validation delegate</description>
    ///   </item>
    ///   <item>
    ///     <description>Configures the instance as read-only to prevent modifications</description>
    ///   </item>
    ///   <item>
    ///     <description>Creates a dynamic proxy that intercepts all method invocations</description>
    ///   </item>
    ///   <item>
    ///     <description>Links the proxy and the proxy manager for proper method interception</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The factory method pattern is used here to ensure proper initialization of the proxy system,
    /// which requires circular references between the proxy and its manager.
    /// </para>
    /// <para>
    /// The method is designed to be called by command providers and internal implementations, not
    /// directly by end users of the API.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="item"/> or <paramref name="validateAsyncDelegate"/> is <see langword="null"/>.
    /// </exception>
    public static ReadResult<TInterface, TItem> Create(
        TItem item,
        ValidateAsyncDelegate<TInterface, TItem> validateAsyncDelegate)
    {
        // Create the proxy manager - need an item reference for the ItemProxy onInvoke delegate
        var proxyManager = new ReadResult<TInterface, TItem>
        {
            _item = item,
            _isReadOnly = true,
            _validateAsyncDelegate = validateAsyncDelegate,
        };

        // Create the proxy that will be exposed to consumers
        var proxy = ItemProxy<TInterface, TItem>.Create(proxyManager.OnInvoke);

        // Set our proxy reference
        proxyManager._proxy = proxy;

        // Return the configured proxy manager
        return proxyManager;
    }

    #endregion
}
