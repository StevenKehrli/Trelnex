using System.Net;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// The interface to save a batch of items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Adds a <see cref="ISaveCommand{TInterface}"/> to the batch.
    /// </summary>
    /// <param name="saveCommand">>The <see cref="ISaveCommand{TInterface}"/> to add to the batch.</param>
    /// <returns>The <see cref="IBatchCommand{TInterface}"/> with the <see cref="ISaveCommand{TInterface}"/> added.</returns>
    IBatchCommand<TInterface> Add(
        ISaveCommand<TInterface> saveCommand);

    /// <summary>
    /// The action to save the batch of items to the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The array of items that were saved.</returns>
    Task<IBatchResult<TInterface>[]> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// The action to validate the batch of items.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The array of fluent <see cref="ValidationResult"/> item that was saved.</returns>
    Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// The class to save a batch of items in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
internal class BatchCommand<TInterface, TItem>(
    SaveBatchAsyncDelegate<TInterface, TItem> saveBatchAsyncDelegate)
    : IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface
{
    /// <summary>
    /// An exclusive lock to ensure that only one operation that modifies the batch is in progress at a time
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// The batch of save commands to save.
    /// </summary>
    private List<SaveCommand<TInterface, TItem>> _saveCommands = [];

    /// <summary>
    /// Adds a <see cref="ISaveCommand{TInterface}"/> to the batch.
    /// </summary>
    /// <param name="saveCommand">>The <see cref="ISaveCommand{TInterface}"/> to add to the batch.</param>
    /// <returns>The <see cref="IBatchCommand{TInterface}"/> with the <see cref="ISaveCommand{TInterface}"/> added.</returns>
    /// <exception cref="ArgumentException">if the <paramref name="saveCommand"/> is not of type <see cref="SaveCommand{TInterface, TItem}"/>.</exception>
    public IBatchCommand<TInterface> Add(
        ISaveCommand<TInterface> saveCommand)
    {
        // ensure that the save command is of the correct type
        if (saveCommand is not SaveCommand<TInterface, TItem> sc)
        {
            throw new ArgumentException(
                $"The {nameof(saveCommand)} must be of type {typeof(SaveCommand<TInterface, TItem>).Name}.",
                nameof(saveCommand));
        }

        // ensure that only one operation that modifies the batch is in progress at a time
        _semaphore.Wait();

        try
        {
            // check if already saved
            if (_saveCommands is null)
            {
                throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
            }

            // add the save command to the batch
            _saveCommands.Add(sc);

            return this;
        }
        finally
        {
            // release the exclusive lock
            _semaphore.Release();
        }
    }

    /// <summary>
    /// The action to save the batch of items to the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"> that invoked this method.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The array of items that were saved.</returns>
    public async Task<IBatchResult<TInterface>[]> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        // ensure that only one operation that modifies the batch is in progress at a time
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            // check if already saved
            if (_saveCommands is null)
            {
                throw new InvalidOperationException("The Command is no longer valid because its SaveAsync method has already been called.");
            }

            // if there are no save commands, return an empty array
            if (_saveCommands.Count == 0)
            {
                return [];
            }

            // validate the save commands
            var validationResultTasks = _saveCommands
                .Select(sc => sc.ValidateAsync(cancellationToken));

            var validationResults = await Task.WhenAll(validationResultTasks);
            validationResults.ValidateOrThrow<TItem>();

            // acquire the save requests from each save command
            var acquireTasks = _saveCommands
                .Select(sc => sc.AcquireAsync(requestContext, cancellationToken))
                .ToArray();

            // wait for the acquire tasks to complete
            // do not propagate the cancellation token
            // each acquire task will handle the cancellation
            Task.WaitAll(acquireTasks, CancellationToken.None);

            // if cancellation requested, release the save commands and throw
            if (cancellationToken.IsCancellationRequested)
            {
                // release the save commands
                _saveCommands.ForEach(sc => sc.Release());

                cancellationToken.ThrowIfCancellationRequested();
            }

            // check if any of the acquire tasks faulted
            return acquireTasks.Any(at => at.IsFaulted)
                ? AcquireTasksFaulted(acquireTasks)
                : await AcquireTasksCompletedSuccessfully(acquireTasks, cancellationToken);
        }
        finally
        {
            // release the exclusive lock
            _semaphore.Release();
        }
    }

    /// <summary>
    /// The action to validate the batch of items.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The array of fluent <see cref="ValidationResult"/> item that was saved.</returns>
    public async Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken = default)
    {
        // ensure that only one operation that modifies the batch is in progress at a time
        await _semaphore.WaitAsync(cancellationToken);

        // validate the save commands
        var validationResultTasks = _saveCommands
            .Select(sc => sc.ValidateAsync(cancellationToken));

        var validationResults = await Task.WhenAll(validationResultTasks);

        // release the exclusive lock
        _semaphore.Release();

        return validationResults;
    }

    /// <summary>
    /// Build the batch results from the acquire tasks - faulted path.
    /// </summary>
    /// <param name="acquireTasks">The acquire tasks from which to build the batch results.</param>
    /// <returns>The array of <see cref="IBatchResult{TInterface}"/>.</returns>
    private IBatchResult<TInterface>[] AcquireTasksFaulted(
        Task<SaveRequest<TInterface, TItem>>[] acquireTasks)
    {
        // allocate the array of batch results
        var batchResults = new BatchResult<TInterface, TItem>[acquireTasks.Length];

        for (var index = 0; index < acquireTasks.Length; index++)
        {
            // get the acquire task
            var acquireTask = acquireTasks[index];

            // if this task completed successfully, release the save command
            if (acquireTask.IsCompletedSuccessfully) _saveCommands[index].Release();

            // bad request: this faulted
            // failed dependency: something else faulted
            batchResults[index] = new BatchResult<TInterface, TItem>(
                httpStatusCode: acquireTask.IsFaulted
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.FailedDependency,
                readResult: null);
        }

        return batchResults;
    }

    /// <summary>
    /// Build the batch results from the acquire tasks - completed successfully path.
    /// </summary>
    /// <param name="acquireTasks">The acquire tasks from which to build the batch results.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The array of <see cref="IBatchResult{TInterface}"/>.</returns>
    private async Task<IBatchResult<TInterface>[]> AcquireTasksCompletedSuccessfully(
        Task<SaveRequest<TInterface, TItem>>[] acquireTasks,
        CancellationToken cancellationToken)
    {
        // allocate the array of save requests
        var requests = acquireTasks
            .Select(at => at.Result)
            .ToArray();

        // get the partition key from the first item
        var partitionKey = requests.First().Item.PartitionKey;

        // save the batch
        var saveResults = await saveBatchAsyncDelegate(
            partitionKey,
            requests,
            cancellationToken);

        // allocate the batch results
        var batchResults = new BatchResult<TInterface, TItem>[_saveCommands.Count];

        // determine if all the save results were successful
        var isCompletedSuccessfully = saveResults.All(sr => sr.HttpStatusCode == HttpStatusCode.OK);

        for (var index = 0; index < saveResults.Length; index++)
        {
            // get the save command and the save result
            var saveCommand = _saveCommands[index];
            var saveResult = saveResults[index];

            // update the save command and get the read result
            var readResult = isCompletedSuccessfully
                ? saveCommand.Update(saveResult.Item!)
                : null;

            // release the save command
            saveCommand.Release();

            // create the batch result
            batchResults[index] = new BatchResult<TInterface, TItem>(
                saveResult.HttpStatusCode,
                readResult);
        }

        // null out the saveCommands so we know that we have already saved and are no longer valid
        _saveCommands = null!;

        return batchResults;
    }
}
