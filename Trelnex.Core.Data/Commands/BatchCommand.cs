using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines operations for managing and executing a batch of save commands.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
public interface IBatchCommand<TItem>
    where TItem : BaseItem
{
    /// <summary>
    /// Adds a save command to the batch for later execution.
    /// </summary>
    /// <param name="saveCommand">The save command to add to the batch.</param>
    /// <returns>The same batch command instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when saveCommand is not the expected concrete type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the batch has already been executed.</exception>
    IBatchCommand<TItem> Add(
        ISaveCommand<TItem> saveCommand);

    /// <summary>
    /// Executes all save commands in the batch as an atomic operation.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the batch operation.</param>
    /// <returns>Array of batch results corresponding to each save command.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the batch has already been executed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<IBatchResult<TItem>[]> SaveAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates all items in the batch without executing the save operations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the validation operation.</param>
    /// <returns>Array of validation results for each item in the batch.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements batch processing for multiple save commands with atomic execution.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem.</typeparam>
/// <param name="saveBatchAsyncDelegate">Delegate that performs the actual batch save operation.</param>
internal class BatchCommand<TItem>(
    SaveBatchAsyncDelegate<TItem> saveBatchAsyncDelegate)
    : IBatchCommand<TItem>
    where TItem : BaseItem
{
    #region Private Fields

    // Semaphore to ensure thread-safe modification of the batch
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // Collection of save commands to execute in the batch, nulled after execution
    private List<SaveCommand<TItem>> _saveCommands = [];

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public IBatchCommand<TItem> Add(
        ISaveCommand<TItem> saveCommand)
    {
        // Verify the save command is the expected concrete type
        if (saveCommand is not SaveCommand<TItem> sc)
        {
            throw new ArgumentException(
                $"The {nameof(saveCommand)} must be of type {typeof(SaveCommand<TItem>).Name}.",
                nameof(saveCommand));
        }

        try
        {
            // Acquire lock to prevent concurrent modification of the batch
            _semaphore.Wait();

            // Ensure the batch hasn't been executed yet
            if (_saveCommands is null)
            {
                throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
            }

            // Add the save command to the batch collection
            _saveCommands.Add(sc);

            return this;
        }
        finally
        {
            // Always release the lock
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IBatchResult<TItem>[]> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Acquire lock to prevent concurrent execution
            await _semaphore.WaitAsync(cancellationToken);

            // Ensure the batch hasn't been executed yet
            if (_saveCommands is null)
            {
                throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
            }

            // Return empty array if no commands to execute
            if (_saveCommands.Count == 0)
            {
                return [];
            }

            // Validate all commands before attempting to save
            var validationResults = await ValidateAsyncInternal(cancellationToken);
            validationResults.ValidateOrThrow<TItem>();

            // Acquire save requests from all commands concurrently
            var acquireTasks = _saveCommands
                .Select(sc => sc.AcquireAsync(cancellationToken))
                .ToArray();

            // Wait for all acquisition attempts to complete
            Task.WaitAll(acquireTasks, CancellationToken.None);

            // Handle cancellation after acquisition attempts
            if (cancellationToken.IsCancellationRequested)
            {
                // Release any successfully acquired commands
                _saveCommands.ForEach(sc => sc.Release());

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Execute batch or handle failures based on acquisition results
            return acquireTasks.Any(at => at.IsFaulted)
                ? RevertBatch(acquireTasks)
                : await SaveBatch(acquireTasks, cancellationToken);
        }
        finally
        {
            // Always release the lock
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Acquire lock to ensure consistent state during validation
            await _semaphore.WaitAsync(cancellationToken);

            // Perform the actual validation
            return await ValidateAsyncInternal(cancellationToken);
        }
        finally
        {
            // Always release the lock
            _semaphore.Release();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates batch results for failed acquisition attempts with appropriate status codes.
    /// </summary>
    /// <param name="acquireTasks">Array of acquisition task results.</param>
    /// <returns>Array of batch results indicating failure reasons.</returns>
    private IBatchResult<TItem>[] RevertBatch(
        Task<SaveRequest<TItem>>[] acquireTasks)
    {
        // Create results array matching the number of commands
        var batchResults = new BatchResult<TItem>[acquireTasks.Length];

        for (var index = 0; index < acquireTasks.Length; index++)
        {
            var acquireTask = acquireTasks[index];

            // Release any successfully acquired commands
            if (acquireTask.IsCompletedSuccessfully)
            {
                _saveCommands[index].Release();
            }

            // Set status code based on whether this specific task failed or another did
            batchResults[index] = new BatchResult<TItem>(
                httpStatusCode: acquireTask.IsFaulted
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.FailedDependency,
                readResult: null);
        }

        return batchResults;
    }

    /// <summary>
    /// Executes the batch save operation when all commands were successfully acquired.
    /// </summary>
    /// <param name="acquireTasks">Successfully completed acquisition tasks.</param>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    /// <returns>Array of batch results from the save operation.</returns>
    private async Task<IBatchResult<TItem>[]> SaveBatch(
        Task<SaveRequest<TItem>>[] acquireTasks,
        CancellationToken cancellationToken)
    {
        // Extract save requests from completed acquisition tasks
        var requests = acquireTasks
            .Select(at => at.Result)
            .ToArray();

        // Execute the batch save using the configured delegate
        var saveResults = await saveBatchAsyncDelegate(
            requests,
            cancellationToken);

        // Create batch results array
        var batchResults = new BatchResult<TItem>[_saveCommands.Count];

        // Check if all save operations succeeded
        var isCompletedSuccessfully = saveResults.All(sr => sr.HttpStatusCode == HttpStatusCode.OK);

        for (var index = 0; index < saveResults.Length; index++)
        {
            var saveCommand = _saveCommands[index];
            var saveResult = saveResults[index];

            // Update command with saved item only if all operations succeeded
            var readResult = isCompletedSuccessfully
                ? saveCommand.Saved(saveResult.Item!)
                : null;

            // Release the command's resources
            saveCommand.Release();

            // Create batch result with save status and optional read result
            batchResults[index] = new BatchResult<TItem>(
                saveResult.HttpStatusCode,
                readResult);
        }

        // Prevent reuse of this batch command
        _saveCommands = null!;

        return batchResults;
    }

    /// <summary>
    /// Validates all commands in the batch and ensures partition key consistency.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the validation operation.</param>
    /// <returns>Array of validation results for each command.</returns>
    public async Task<ValidationResult[]> ValidateAsyncInternal(
        CancellationToken cancellationToken = default)
    {
        // Return empty array for empty batch
        if (_saveCommands.Count == 0)
        {
            return [];
        }

        // Use first item's partition key as the required batch partition key
        var partitionKey = _saveCommands.First().Item.PartitionKey;

        // Create validator to ensure all items have the same partition key
        var validator = new InlineValidator<TItem>();
        validator.RuleFor(item => item.PartitionKey)
            .Must(pk => string.Equals(pk, partitionKey))
            .WithMessage(item => $"The partition key '{item.PartitionKey}' does not match the batch partition key '{partitionKey}'.");

        // Validate both partition key consistency and individual command rules
        var validationResults = await Task.WhenAll(_saveCommands.Select(async sc =>
        {
            var vrPartitionKey = await validator.ValidateAsync(sc.Item, cancellationToken);
            var vrSaveCommand = await sc.ValidateAsync(cancellationToken);

            if (vrPartitionKey.IsValid) return vrSaveCommand;
            if (vrSaveCommand.IsValid) return vrPartitionKey;

            // Combine errors from both validations if both failed
            return new ValidationResult(vrPartitionKey.Errors.Concat(vrSaveCommand.Errors));
        }));

        return validationResults;
    }

    #endregion
}
