using FluentValidation;

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

    /// <inheritdoc/>
    public IDataProvider<TInterface> Create<TInterface, TItem>(
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        where TInterface : class, IBaseItem
        where TItem : BaseItem, TInterface, new()
    {
        return new InMemoryDataProvider<TInterface, TItem>(
            typeName,
            validator,
            commandOperations);
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
