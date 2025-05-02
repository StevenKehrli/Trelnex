using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// A factory for creating instances of <see cref="InMemoryCommandProvider{TInterface, TItem}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This factory provides an in-memory implementation of the command provider pattern, useful for:
/// - Unit testing scenarios where persistence is not needed
/// - Prototyping without database configuration
/// - Temporary data storage in single-instance applications
/// </para>
/// <para>
/// The in-memory provider serializes items to JSON strings to validate proper JSON attribute configuration,
/// simulating behavior of persistent stores like Cosmos DB while keeping everything in memory.
/// </para>
/// </remarks>
public class InMemoryCommandProviderFactory : ICommandProviderFactory
{
    #region Private Fields

    /// <summary>
    /// Function that returns the current status of the factory.
    /// </summary>
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryCommandProviderFactory"/> class.
    /// </summary>
    /// <param name="getStatus">The function that returns the current status of the factory.</param>
    private InMemoryCommandProviderFactory(
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _getStatus = getStatus;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new instance of the <see cref="InMemoryCommandProviderFactory"/>.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains
    /// the new <see cref="InMemoryCommandProviderFactory"/> instance.
    /// </returns>
    /// <remarks>
    /// This factory is always created in a healthy state as it doesn't depend on
    /// external resources that could fail.
    /// </remarks>
    public static async Task<InMemoryCommandProviderFactory> Create()
    {
        CommandProviderFactoryStatus getStatus()
        {
            return new CommandProviderFactoryStatus(
                IsHealthy: true,
                Data: new Dictionary<string, object>());
        }

        var factory = new InMemoryCommandProviderFactory(
            getStatus);

        return await Task.FromResult(factory);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates an instance of the <see cref="InMemoryCommandProvider{TInterface, TItem}"/>.
    /// </summary>
    /// <typeparam name="TInterface">The interface type that defines the contract for the item.</typeparam>
    /// <typeparam name="TItem">The concrete item type that implements the specified interface.</typeparam>
    /// <param name="typeName">
    /// The type name of the item to be stored in the <see cref="BaseItem.TypeName"/> property.
    /// Must follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.
    /// </param>
    /// <param name="validator">
    /// The fluent validator for domain-specific validation of the item.
    /// Pass <see langword="null"/> to skip domain validation.
    /// </param>
    /// <param name="commandOperations">
    /// Specifies which operations (Update/Delete) are allowed.
    /// By default, Update is allowed but Delete is not.
    /// </param>
    /// <returns>
    /// An <see cref="ICommandProvider{TInterface}"/> implementation that stores data in memory.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="typeName"/> does not follow the naming rules or is a reserved type name.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="typeName"/> is <see langword="null"/> or empty.
    /// </exception>
    public ICommandProvider<TInterface> Create<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        return new InMemoryCommandProvider<TInterface, TItem>(
            typeName,
            validator,
            commandOperations);
    }

    /// <summary>
    /// Gets the current operational status of the command provider factory.
    /// </summary>
    /// <returns>
    /// A <see cref="CommandProviderFactoryStatus"/> indicating if the factory is healthy
    /// and containing additional status information.
    /// </returns>
    /// <remarks>
    /// The in-memory provider is always considered healthy since it doesn't depend on
    /// external resources that could fail.
    /// </remarks>
    public CommandProviderFactoryStatus GetStatus() => _getStatus();

    #endregion
}
