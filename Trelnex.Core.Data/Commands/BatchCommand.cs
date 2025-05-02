using System.Net;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines an interface to save a batch of items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Adds a save command to the batch.
    /// </summary>
    /// <param name="saveCommand">The <see cref="ISaveCommand{TInterface}"/> to add to the batch.</param>
    /// <returns>The current <see cref="IBatchCommand{TInterface}"/> instance with the command added.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="saveCommand"/> is not of the expected type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the command has already been saved.</exception>
    IBatchCommand<TInterface> Add(
        ISaveCommand<TInterface> saveCommand);

    /// <summary>
    /// Saves the batch of items to the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"/> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An array of <see cref="IBatchResult{TInterface}"/> objects representing the save operation results.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the command has already been saved.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    Task<IBatchResult<TInterface>[]> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates the batch of items without saving them.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An array of <see cref="ValidationResult"/> objects for each item in the batch.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements functionality to save a batch of items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items.</typeparam>
/// <remarks>
/// This class manages concurrency through a semaphore to ensure thread safety during batch operations.
/// Once SaveAsync is called, the command becomes invalid for further use.
/// </remarks>
/// <param name="saveBatchAsyncDelegate">The delegate used to save the batch to the data store.</param>
internal class BatchCommand<TInterface, TItem>(
    SaveBatchAsyncDelegate<TInterface, TItem> saveBatchAsyncDelegate)
    : IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    #region Private Fields

    /// <summary>
    /// An exclusive lock to ensure that only one operation that modifies the batch is in progress at a time.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// The batch of save commands to save. Set to null after saving to prevent reuse.
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

        // Ensure that only one operation that modifies the batch is in progress at a time
        _semaphore.Wait();

        try
        {
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
        IRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        // Ensure that only one operation that modifies the batch is in progress at a time
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
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

            // Validate all save commands in parallel
            var validationResultTasks = _saveCommands
                .Select(sc => sc.ValidateAsync(cancellationToken));

            var validationResults = await Task.WhenAll(validationResultTasks);
            validationResults.ValidateOrThrow<TItem>();

            // Acquire the save requests from each save command in parallel
            var acquireTasks = _saveCommands
                .Select(sc => sc.AcquireAsync(requestContext, cancellationToken))
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
                ? AcquireTasksFaulted(acquireTasks)
                : await AcquireTasksCompletedSuccessfully(acquireTasks, cancellationToken);
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
        // Ensure that only one operation that modifies the batch is in progress at a time
        await _semaphore.WaitAsync(cancellationToken);

        // Validate the save commands in parallel
        var validationResultTasks = _saveCommands
            .Select(sc => sc.ValidateAsync(cancellationToken));

        var validationResults = await Task.WhenAll(validationResultTasks);
        // Release the exclusive lock
        _semaphore.Release();

        return validationResults;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Builds batch results when one or more acquire tasks have faulted.
    /// </summary>
    /// <param name="acquireTasks">The acquire tasks from which to build the batch results.</param>
    /// <returns>An array of batch results with appropriate status codes.</returns>
    /// <remarks>
    /// Tasks that completed successfully are released, and appropriate HTTP status codes are
    /// assigned based on whether the individual task faulted or another task in the batch faulted.
    /// </remarks>
    private IBatchResult<TInterface>[] AcquireTasksFaulted(
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
    /// Builds batch results when all acquire tasks completed successfully.
    /// </summary>
    /// <param name="acquireTasks">The acquire tasks from which to build the batch results.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An array of batch results with items and status codes.</returns>
    /// <remarks>
    /// This method performs the actual save operation through the save batch delegate and
    /// processes the results to build the final batch results.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    private async Task<IBatchResult<TInterface>[]> AcquireTasksCompletedSuccessfully(
        Task<SaveRequest<TInterface, TItem>>[] acquireTasks,
        CancellationToken cancellationToken)
    {
        // Extract the save requests from completed tasks
        var requests = acquireTasks
            .Select(at => at.Result)
            .ToArray();

        // Get the partition key from the first item
        var partitionKey = requests.First().Item.PartitionKey;

        // Execute the batch save operation
        var saveResults = await saveBatchAsyncDelegate(
            partitionKey,
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

    #endregion
}
