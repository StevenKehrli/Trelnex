using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for accessing an item and creating commands for it.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface IQueryResult<TItem>
    : IDisposable
    where TItem : BaseItem
{
    /// <summary>
    /// Gets the managed item.
    /// </summary>
    TItem Item { get; }

    /// <summary>
    /// Creates a delete command for the item and invalidates this query result.
    /// </summary>
    /// <returns>A save command configured for deleting the item.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Delete() or Update() has already been called.
    /// </exception>
    ISaveCommand<TItem> Delete();

    /// <summary>
    /// Creates an update command for the item and invalidates this query result.
    /// </summary>
    /// <returns>A save command configured for updating the item.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if Update() or Delete() has already been called.
    /// </exception>
    ISaveCommand<TItem> Update();
}

/// <summary>
/// Manages an item and provides factory methods for creating save commands.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
internal class QueryResult<TItem>
    : ItemManager<TItem>, IQueryResult<TItem>
    where TItem : BaseItem
{
    #region Private Fields

    // Factory function for creating delete commands, nulled after use to invalidate
    private Func<TItem, ISaveCommand<TItem>> _createDeleteCommand;

    // Factory function for creating update commands, nulled after use to invalidate
    private Func<TItem, ISaveCommand<TItem>> _createUpdateCommand;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the query result with the specified item, command factories, and optional logger.
    /// </summary>
    /// <param name="item">The item to manage.</param>
    /// <param name="createDeleteCommand">Factory for creating delete commands.</param>
    /// <param name="createUpdateCommand">Factory for creating update commands.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    private QueryResult(
        TItem item,
        Func<TItem, ISaveCommand<TItem>> createDeleteCommand,
        Func<TItem, ISaveCommand<TItem>> createUpdateCommand,
        ILogger? logger = null)
        : base(item, logger)
    {
        _createDeleteCommand = createDeleteCommand;
        _createUpdateCommand = createUpdateCommand;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Factory method that creates a new query result instance.
    /// </summary>
    /// <param name="item">The item to manage.</param>
    /// <param name="createDeleteCommand">Factory for creating delete commands.</param>
    /// <param name="createUpdateCommand">Factory for creating update commands.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>A configured query result instance.</returns>
    public static QueryResult<TItem> Create(
        TItem item,
        Func<TItem, ISaveCommand<TItem>> createDeleteCommand,
        Func<TItem, ISaveCommand<TItem>> createUpdateCommand,
        ILogger? logger = null)
    {
        return new QueryResult<TItem>(
            item: item,
            createDeleteCommand: createDeleteCommand,
            createUpdateCommand: createUpdateCommand,
            logger: logger);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public ISaveCommand<TItem> Delete()
    {
        try
        {
            // Acquire lock to prevent concurrent transitions
            Wait();

            // Check if command factories have already been used
            if (_createDeleteCommand is null)
            {
                throw new InvalidOperationException("The Delete() method cannot be called because either the Delete() or Update() method has already been called.");
            }

            var deleteCommand = _createDeleteCommand(Item);

            // Invalidate this query result by clearing both command factories
            _createDeleteCommand = null!;
            _createUpdateCommand = null!;

            return deleteCommand;
        }
        finally
        {
            Release();
        }
    }

    /// <inheritdoc/>
    public ISaveCommand<TItem> Update()
    {
        try
        {
            // Acquire lock to prevent concurrent transitions
            Wait();

            // Check if command factories have already been used
            if (_createUpdateCommand is null)
            {
                throw new InvalidOperationException("The Update() method cannot be called because either the Delete() or Update() method has already been called.");
            }

            var updateCommand = _createUpdateCommand(Item);

            // Invalidate this query result by clearing both command factories
            _createDeleteCommand = null!;
            _createUpdateCommand = null!;

            return updateCommand;
        }
        finally
        {
            Release();
        }
    }

    #endregion
}
