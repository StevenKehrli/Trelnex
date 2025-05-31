using System.Net;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Saves a batch of items atomically.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items.</typeparam>
/// <remarks>
/// Processes multiple data operations as a single transaction.
/// </remarks>
public interface IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Adds a save command to the batch.
    /// </summary>
    /// <param name="saveCommand">The save command to add.</param>
    /// <returns>The current batch command instance.</returns>
    /// <exception cref="ArgumentException">Thrown when saveCommand is not of the expected type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the batch command has already been executed.</exception>
    IBatchCommand<TInterface> Add(
        ISaveCommand<TInterface> saveCommand);

    /// <summary>
    /// Executes the batch as a single atomic transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Array of batch results.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when already executed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when canceled.</exception>
    Task<IBatchResult<TInterface>[]> SaveAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates batch items without saving them.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Array of validation results.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when canceled.</exception>
    Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements atomic batch operations.
/// </summary>
/// <typeparam name="TInterface">Interface type of the items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Handles collection, validation, and atomic execution of multiple data operations.
/// </remarks>
/// <param name="saveBatchAsyncDelegate">Delegate for storage-specific operations.</param>
internal class BatchCommand<TInterface, TItem>(
    SaveBatchAsyncDelegate<TInterface, TItem> saveBatchAsyncDelegate)
    : IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// Ensures only one batch-modifying operation runs at a time.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Collection of save commands for this batch.
    /// </summary>
    private List<SaveCommand<TInterface, TItem>> _saveCommands = [];

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public IBatchCommand<TInterface> Add(
        ISaveCommand<TInterface> saveCommand)
    {
        // Ensure that the save command is of the correct type
        if (saveCommand is not SaveCommand<TInterface, TItem> sc)
        {
            throw new ArgumentException(
                $"The {nameof(saveCommand)} must be of type {typeof(SaveCommand<TInterface, TItem>).Name}.",
                nameof(saveCommand));
        }

        try
        {
            // Ensure that only one operation that modifies the batch is in progress at a time
            _semaphore.Wait();

            // Check if already saved
            if (_saveCommands is null)
            {
                throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
            }

            // Add the save command to the batch
            _saveCommands.Add(sc);

            return this;
        }
        finally
        {
            // Release the exclusive lock
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IBatchResult<TInterface>[]> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure that only one operation that modifies the batch is in progress at a time
            await _semaphore.WaitAsync(cancellationToken);

            // Check if already saved
            if (_saveCommands is null)
            {
                throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
            }

            // If there are no save commands, return an empty array
            if (_saveCommands.Count == 0)
            {
                return [];
            }

            // Validate the save commands
            var validationResults = await ValidateAsyncInternal(cancellationToken);
            validationResults.ValidateOrThrow<TItem>();

            // Acquire the save requests from each save command in parallel
            var acquireTasks = _saveCommands
                .Select(sc => sc.AcquireAsync(cancellationToken))
                .ToArray();

            // Wait for the acquire tasks to complete
            // Do not propagate the cancellation token
            // Each acquire task will handle the cancellation internally
            Task.WaitAll(acquireTasks, CancellationToken.None);

            // If cancellation requested, release the save commands and throw
            if (cancellationToken.IsCancellationRequested)
            {
                // Release all save commands to prevent resource leaks
                _saveCommands.ForEach(sc => sc.Release());

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Process results based on whether any acquire tasks faulted
            return acquireTasks.Any(at => at.IsFaulted)
                ? RevertBatch(acquireTasks)
                : await SaveBatch(acquireTasks, cancellationToken);
        }
        finally
        {
            // Release the exclusive lock
            _semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure that only one operation that modifies the batch is in progress at a time
            await _semaphore.WaitAsync(cancellationToken);

            // Validate the save commands
            return await ValidateAsyncInternal(cancellationToken);
        }
        finally
        {
            // Always release the exclusive lock
            _semaphore.Release();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles failed item acquisition and assigns appropriate status codes.
    /// </summary>
    /// <param name="acquireTasks">Array of acquire tasks.</param>
    /// <returns>Batch results with failure status codes.</returns>
    private IBatchResult<TInterface>[] RevertBatch(
        Task<SaveRequest<TInterface, TItem>>[] acquireTasks)
    {
        // Allocate the array of batch results
        var batchResults = new BatchResult<TInterface, TItem>[acquireTasks.Length];

        for (var index = 0; index < acquireTasks.Length; index++)
        {
            // Get the acquire task
            var acquireTask = acquireTasks[index];

            // If this task completed successfully, release the save command
            if (acquireTask.IsCompletedSuccessfully)
            {
                _saveCommands[index].Release();
            }

            // Assign appropriate status code:
            // - Bad request (400) if this specific task faulted
            // - Failed dependency (424) if another task in the batch faulted
            batchResults[index] = new BatchResult<TInterface, TItem>(
                httpStatusCode: acquireTask.IsFaulted
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.FailedDependency,
                readResult: null);
        }

        return batchResults;
    }

    /// <summary>
    /// Processes batch when all items were successfully acquired.
    /// </summary>
    /// <param name="acquireTasks">Successfully completed acquisition tasks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Batch results with status codes.</returns>
    /// <exception cref="OperationCanceledException">Thrown when canceled.</exception>
    private async Task<IBatchResult<TInterface>[]> SaveBatch(
        Task<SaveRequest<TInterface, TItem>>[] acquireTasks,
        CancellationToken cancellationToken)
    {
        // Extract the save requests from completed tasks
        var requests = acquireTasks
            .Select(at => at.Result)
            .ToArray();

        // Execute the batch save operation
        var saveResults = await saveBatchAsyncDelegate(
            requests,
            cancellationToken);

        // Allocate the batch results
        var batchResults = new BatchResult<TInterface, TItem>[_saveCommands.Count];

        // Determine if all the save results were successful
        var isCompletedSuccessfully = saveResults.All(sr => sr.HttpStatusCode == HttpStatusCode.OK);

        for (var index = 0; index < saveResults.Length; index++)
        {
            // Get the save command and the save result
            var saveCommand = _saveCommands[index];
            var saveResult = saveResults[index];

            // Update the save command and get the read result if all operations succeeded
            var readResult = isCompletedSuccessfully
                ? saveCommand.Update(saveResult.Item!)
                : null;

            // Release the save command
            saveCommand.Release();

            // Create the batch result with appropriate status code and result
            batchResults[index] = new BatchResult<TInterface, TItem>(
                saveResult.HttpStatusCode,
                readResult);
        }

        // Invalidate the batch command to prevent reuse
        _saveCommands = null!;

        return batchResults;
    }

    /// <summary>
    /// Internal validation implementation.
    /// </summary>
    public async Task<ValidationResult[]> ValidateAsyncInternal(
        CancellationToken cancellationToken = default)
    {
        // Return empty array for empty batch
        if (_saveCommands.Count == 0)
        {
            return [];
        }

        // Get partition key from first item
        var partitionKey = _saveCommands.First().Item.PartitionKey;

        // Create validator for partition key consistency
        var validator = new InlineValidator<TInterface>();
        validator.RuleFor(item => item.PartitionKey)
            .Must(pk => string.Equals(pk, partitionKey))
            .WithMessage(item => $"The partition key '{item.PartitionKey}' does not match the batch partition key '{partitionKey}'.");

        // Validate partition key consistency and command rules in parallel
        var validationResults = await Task.WhenAll(_saveCommands.Select(async sc =>
        {
            var vrPartitionKey = await validator.ValidateAsync(sc.Item, cancellationToken);
            var vrSaveCommand = await sc.ValidateAsync(cancellationToken);

            if (vrPartitionKey.IsValid) return vrSaveCommand;
            if (vrSaveCommand.IsValid) return vrPartitionKey;

            // Merge errors if both validations failed
            return new ValidationResult(vrPartitionKey.Errors.Concat(vrSaveCommand.Errors));
        }));

        return validationResults;
    }

    #endregion
}
