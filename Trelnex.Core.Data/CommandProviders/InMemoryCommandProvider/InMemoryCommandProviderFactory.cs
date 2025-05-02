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
    #region Private Fields

    /// <summary>
    /// Status provider function.
    /// </summary>
    private readonly Func<CommandProviderFactoryStatus> _getStatus;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes factory instance.
    /// </summary>
    /// <param name="getStatus">Status provider function.</param>
    private InMemoryCommandProviderFactory(
        Func<CommandProviderFactoryStatus> getStatus)
    {
        _getStatus = getStatus;
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
    public CommandProviderFactoryStatus GetStatus() => _getStatus();

    #endregion
}
