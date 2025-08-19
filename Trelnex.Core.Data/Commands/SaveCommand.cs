using System.Text.Json.Nodes;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for a command that can save an item and validate it.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface ISaveCommand<TItem>
    : IDisposable
    where TItem : BaseItem
{
    /// <summary>
    /// Gets the item managed by this command.
    /// </summary>
    TItem Item { get; }

    /// <summary>
    /// Saves the item using the configured save delegate and returns a result wrapper.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A result wrapper for the saved item.</returns>
    Task<IReadResult<TItem>> SaveAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the item using the configured validation delegate.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Manages an item that can be saved, with change tracking and validation capabilities.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
internal class SaveCommand<TItem>
    : ItemManager<TItem>, ISaveCommand<TItem>
    where TItem : BaseItem
{
    #region Private Fields

    // JSON representation of the item used to track changes from the baseline
    private JsonNode? _changesAsJsonNode;

    // The type of save operation to perform
    private SaveAction _saveAction;

    // Delegate that performs the actual save operation, nulled after use to invalidate command
    private SaveAsyncDelegate<TItem> _saveAsyncDelegate;

    // Function that serializes the item to JSON for change tracking
    private Func<TItem, JsonNode?> _serializeChanges = null!;

    /// <summary>
    /// Delegate that performs asynchronous validation of the item.
    /// </summary>
    protected ValidateAsyncDelegate<TItem> _validateAsyncDelegate;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the save command with all required dependencies and captures the initial state.
    /// </summary>
    /// <param name="item">The item to manage.</param>
    /// <param name="saveAction">The type of save operation.</param>
    /// <param name="serializeChanges">Function to serialize the item for change tracking.</param>
    /// <param name="validateAsyncDelegate">Delegate for item validation.</param>
    /// <param name="saveAsyncDelegate">Delegate for save operation.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    private SaveCommand(
        TItem item,
        SaveAction saveAction,
        Func<TItem, JsonNode?> serializeChanges,
        ValidateAsyncDelegate<TItem> validateAsyncDelegate,
        SaveAsyncDelegate<TItem> saveAsyncDelegate,
        ILogger? logger = null)
        : base(item, logger)
    {
        _saveAction = saveAction;
        _serializeChanges = serializeChanges;
        _validateAsyncDelegate = validateAsyncDelegate;
        _saveAsyncDelegate = saveAsyncDelegate;

        // Establish the baseline for change tracking
        _changesAsJsonNode = _serializeChanges(item);
    }

    #endregion

    #region Internal Static Methods

    /// <summary>
    /// Factory method that creates a new save command instance.
    /// </summary>
    /// <param name="item">The item to manage.</param>
    /// <param name="saveAction">The type of save operation.</param>
    /// <param name="serializeChanges">Function to serialize the item for change tracking.</param>
    /// <param name="validateAsyncDelegate">Delegate for validation.</param>
    /// <param name="saveAsyncDelegate">Delegate for save operation.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>A configured save command instance.</returns>
    internal static SaveCommand<TItem> Create(
        TItem item,
        SaveAction saveAction,
        Func<TItem, JsonNode?> serializeChanges,
        ValidateAsyncDelegate<TItem> validateAsyncDelegate,
        SaveAsyncDelegate<TItem> saveAsyncDelegate,
        ILogger? logger = null)
    {
        return new SaveCommand<TItem>(
            item: item,
            saveAction: saveAction,
            serializeChanges: serializeChanges,
            validateAsyncDelegate: validateAsyncDelegate,
            saveAsyncDelegate: saveAsyncDelegate,
            logger: logger);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task<IReadResult<TItem>> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Acquire lock to prevent concurrent access during save operation
            await WaitAsync(cancellationToken);

            var request = CreateSaveRequest();

            // Validate before attempting to save
            var validationResult = await ValidateAsync(cancellationToken);
            validationResult.ValidateOrThrow<TItem>();

            // Execute the save operation using the configured delegate
            var item = await _saveAsyncDelegate(
                request,
                cancellationToken);

            return Saved(item);
        }
        finally
        {
            // Always release lock regardless of success or failure
            Release();
        }
    }

    /// <summary>
    /// Validates the managed item using the configured validation delegate.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the validation operation.</param>
    /// <returns>Validation result indicating success or failure with details.</returns>
    public async Task<ValidationResult> ValidateAsync(
        CancellationToken cancellationToken)
    {
        return await _validateAsyncDelegate(Item, cancellationToken);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Acquires the lock and returns a save request for batch processing.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A save request for the current item state.</returns>
    internal async Task<SaveRequest<TItem>> AcquireAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Acquire lock for batch operation
            await WaitAsync(cancellationToken);

            return CreateSaveRequest();
        }
        catch
        {
            // Release lock if request creation fails
            Release();

            throw;
        }
    }

    /// <summary>
    /// Compares the current item state against the baseline to detect changes.
    /// </summary>
    /// <returns>Array of property changes, or null if no changes detected.</returns>
    internal PropertyChange[]? GetPropertyChanges()
    {
        if (_changesAsJsonNode is null) return null;

        var currentJsonNode = _serializeChanges(Item);
        if (currentJsonNode is null) return null;

        return PropertyChanges.Compare(
            initialJsonNode: _changesAsJsonNode,
            currentJsonNode: currentJsonNode);
    }

    /// <summary>
    /// Updates the managed item, resets change tracking baseline, and invalidates the command.
    /// </summary>
    /// <param name="item">The item returned from the save operation.</param>
    /// <returns>A result wrapper for the saved item.</returns>
    internal ReadResult<TItem> Saved(
        TItem item)
    {
        Item = item;
        _changesAsJsonNode = _serializeChanges(item);

        // Invalidate the command by clearing the save delegate
        _saveAsyncDelegate = null!;

        return ReadResult<TItem>.Create(
            item: item,
            logger: _logger);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a save request containing the item, event, and operation type.
    /// </summary>
    /// <returns>A save request for the current item state.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the command has already been used.</exception>
    private SaveRequest<TItem> CreateSaveRequest()
    {
        // Check if command has already been executed
        if (_saveAsyncDelegate is null)
        {
            throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
        }

        // Create event record for the operation
        ItemEvent? createItemEvent()
        {
            if (_changesAsJsonNode is null) return null;

            var changes = GetPropertyChanges();

            return ItemEvent.Create(
                relatedItem: Item,
                saveAction: _saveAction,
                changes: changes);
        }

        var itemEvent = createItemEvent();

        return new SaveRequest<TItem>(
            Item: Item,
            Event: itemEvent,
            SaveAction: _saveAction);
    }

    #endregion
}
