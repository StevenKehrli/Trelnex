using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// Factory for creating in-memory command providers.
/// </summary>
/// <remarks>
/// Provides non-persistent implementation.
/// </remarks>
public class InMemoryCommandProviderFactory : ICommandProviderFactory
{
    #region Constructors

    /// <summary>
    /// Initializes factory instance.
    /// </summary>
    private InMemoryCommandProviderFactory()
    {
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates new factory instance.
    /// </summary>
    /// <returns>Task with new factory instance.</returns>
    /// <remarks>
    /// Always created in healthy state.
    /// </remarks>
#pragma warning disable CS1998
    public static async Task<InMemoryCommandProviderFactory> Create()
    {
        var factory = new InMemoryCommandProviderFactory();

        return factory;
    }
#pragma warning restore CS1998

    #endregion

    #region Public Methods

    /// <inheritdoc/>
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

    /// <inheritdoc/>
#pragma warning disable CS1998
    public async Task<CommandProviderFactoryStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var status = new CommandProviderFactoryStatus(
            IsHealthy: true,
            Data: new Dictionary<string, object>());

        return status;
    }
#pragma warning restore CS1998

    #endregion
}
