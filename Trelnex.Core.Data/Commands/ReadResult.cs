using Microsoft.Extensions.Logging;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for accessing an item.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface IReadResult<TItem>
    : IDisposable
    where TItem : BaseItem
{
    /// <summary>
    /// Gets the managed item.
    /// </summary>
    TItem Item { get; }
}

/// <summary>
/// Manages an item through a result wrapper.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
internal class ReadResult<TItem>
    : ItemManager<TItem>, IReadResult<TItem>
    where TItem : BaseItem
{
    #region Constructor

    /// <summary>
    /// Initializes the read result with the specified item and optional logger.
    /// </summary>
    /// <param name="item">The item to manage.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    private ReadResult(
        TItem item,
        ILogger? logger = null)
        : base(item, logger)
    {
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Factory method that creates a new read result instance.
    /// </summary>
    /// <param name="item">The item to wrap.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>A configured read result instance.</returns>
    internal static ReadResult<TItem> Create(
        TItem item,
        ILogger? logger = null)
    {
        return new ReadResult<TItem>(
            item: item,
            logger: logger);
    }

    #endregion
}
