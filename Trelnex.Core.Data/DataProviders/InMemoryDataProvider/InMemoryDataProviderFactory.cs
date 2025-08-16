using FluentValidation;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// Factory for creating in-memory data provider instances.
/// </summary>
public class InMemoryDataProviderFactory : IDataProviderFactory
{
    #region Constructors

    /// <summary>
    /// Initializes a new factory instance.
    /// </summary>
    private InMemoryDataProviderFactory()
    {
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates a new factory instance.
    /// </summary>
    /// <returns>A task containing the factory instance.</returns>
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
    /// Creates an in-memory data provider for the specified item type.
    /// </summary>
    /// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
    /// <param name="typeName">Type name identifier for the items.</param>
    /// <param name="itemValidator">Optional validator for items.</param>
    /// <param name="commandOperations">Allowed operations for this provider.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive data.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>A configured in-memory data provider instance.</returns>
    public IDataProvider<TItem> Create<TItem>(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
        where TItem : BaseItem, new()
    {
        return new InMemoryDataProvider<TItem>(
            typeName: typeName,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            blockCipherService: blockCipherService,
            logger: logger);
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
