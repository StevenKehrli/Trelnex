using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base class that manages an item with change tracking and thread safety.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
internal abstract class ItemManager<TItem> : IDisposable
    where TItem : BaseItem
{
    #region Private Static Fields

    // Empty JSON node used as default when serialization returns null
    private static readonly JsonNode _jsonNodeEmpty = new JsonObject();

    #endregion

    #region Private Readonly Fields

    // Semaphore to ensure thread-safe access to operations
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    #endregion

    #region Private Fields

    // Flag to track disposal state and prevent multiple disposals
    private bool _disposed = false;

    // JSON representation of the item used to detect changes
    private JsonNode _itemAsJsonNode;

    // The actual item instance being managed
    private TItem _item;

    #endregion

    #region Protected Properties

    /// <summary>
    /// Optional logger for recording warnings and diagnostic information.
    /// </summary>
    protected ILogger? _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the item manager with the specified item and optional logger.
    /// </summary>
    /// <param name="item">The item to manage.</param>
    /// <param name="logger">Optional logger for warnings and diagnostics.</param>
    protected ItemManager(
        TItem item,
        ILogger? logger = null)
    {
        // Store the item and capture its serialized state for change tracking
        _item = item;
        _itemAsJsonNode = JsonSerializer.SerializeToNode(item) ?? _jsonNodeEmpty;

        _logger = logger;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the managed item. When set, updates the change tracking baseline.
    /// </summary>
    public TItem Item
    {
        get => _item;
        set
        {
            _item = value;
            // Update the change tracking baseline to the new item's state
            _itemAsJsonNode = JsonSerializer.SerializeToNode(value) ?? _jsonNodeEmpty;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Disposes the item manager and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Releases the semaphore to allow other operations to proceed.
    /// </summary>
    internal void Release()
    {
        _semaphore.Release();
    }

    /// <summary>
    /// Synchronously acquires the semaphore for exclusive access.
    /// </summary>
    internal void Wait()
    {
        _semaphore.Wait();
    }

    /// <summary>
    /// Asynchronously acquires the semaphore for exclusive access.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>Task that completes when the semaphore is acquired.</returns>
    internal Task WaitAsync(
        CancellationToken cancellationToken = default)
    {
        return _semaphore.WaitAsync(cancellationToken);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Releases managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">True when called from Dispose(), false when called from finalizer.</param>
    protected virtual void Dispose(
        bool disposing)
    {
        if (_disposed) return;

        WarnIfModified();

        if (disposing)
        {
            // Dispose managed resources
            _semaphore.Dispose();
        }

        _disposed = true;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Compares the current item state to the baseline and logs a warning if changes are detected.
    /// </summary>
    private void WarnIfModified()
    {
        var propertyChanges = PropertyChanges.Compare(
            initialJsonNode: _itemAsJsonNode,
            currentJsonNode: JsonSerializer.SerializeToNode(_item) ?? _jsonNodeEmpty);

        if (propertyChanges?.Length > 0)
        {
            // Log warning if the item has been modified since the baseline was set
            _logger?.LogWarning(
                "Item id = '{id}' partitionKey = '{partitionKey}' was modified.",
                _item.Id,
                _item.PartitionKey);
        }
    }

    #endregion

    #region Finalizer

    /// <summary>
    /// Finalizer that ensures resources are cleaned up if Dispose was not called.
    /// </summary>
    ~ItemManager()
    {
        Dispose(false);
    }

    #endregion
}
