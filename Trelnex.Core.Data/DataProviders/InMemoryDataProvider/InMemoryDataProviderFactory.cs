using FluentValidation;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// Factory for creating in-memory data providers for testing and development scenarios.
/// </summary>
/// <remarks>
/// Provides non-persistent, thread-safe data storage implementation suitable for unit testing and prototyping.
/// </remarks>
public class InMemoryDataProviderFactory : IDataProviderFactory
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the factory.
    /// </summary>
    private InMemoryDataProviderFactory()
    {
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new factory instance asynchronously.
    /// </summary>
    /// <returns>A task containing the initialized factory instance.</returns>
    /// <remarks>
    /// The factory is always created in a healthy operational state since it requires no external dependencies.
    /// </remarks>
#pragma warning disable CS1998
    public static async Task<InMemoryDataProviderFactory> Create()
    {
        var factory = new InMemoryDataProviderFactory();

        return factory;
    }
#pragma warning restore CS1998

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a data provider for a specific item type.
    /// </summary>
    /// <typeparam name="TInterface">Interface type for the items.</typeparam>
    /// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
    /// <param name="tableName">Name of the DynamoDB table to use.</param>
    /// <param name="typeName">Type name to filter items by.</param>
    /// <param name="itemValidator">Optional validator for items.</param>
    /// <param name="commandOperations">Operations allowed for this provider.</param>
    /// <param name="blockCipherService">Optional block cipher service for encrypting sensitive data.</param>
    /// <returns>A configured <see cref="IDataProvider{TInterface}"/> instance.</returns>
    /// <remarks>
    /// Creates a <see cref="InMemoryDataProvider{TInterface, TItem}"/> that operates on the in-memory data store.
    /// </remarks>
    public IDataProvider<TInterface> Create<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        IBlockCipherService? blockCipherService = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        return new InMemoryDataProvider<TInterface, TItem>(
            typeName: typeName,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            blockCipherService: blockCipherService);
    }

    /// <inheritdoc/>
#pragma warning disable CS1998
    public async Task<DataProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var status = new DataProviderFactoryStatus(
            IsHealthy: true,
            Data: new Dictionary<string, object>());

        return status;
    }
#pragma warning restore CS1998

    #endregion
}
