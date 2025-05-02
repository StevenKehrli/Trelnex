using System.Net;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines an interface to save a batch of items atomically in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="IBatchCommand{TInterface}"/> interface enables atomic operations for processing
/// multiple data operations as a single transaction. This ensures that either all operations succeed
/// or all operations fail together, maintaining data consistency.
/// </para>
/// <para>
/// Batch commands support adding multiple <see cref="ISaveCommand{TInterface}"/> instances through the
/// <see cref="Add"/> method, which can include different operation types (create, update, delete).
/// Once all operations are added, they are executed atomically with the <see cref="SaveAsync"/> method.
/// </para>
/// <para>
/// A key constraint when using batch commands is that all items in the batch must share the same partition key,
/// which is necessary to ensure atomicity in distributed data stores. This constraint is enforced during execution.
/// </para>
/// <para>
/// The command can optionally be validated without persisting changes using the <see cref="ValidateAsync"/> method.
/// This allows pre-flight validation checks before committing to a batch operation.
/// </para>
/// <para>
/// After calling <see cref="SaveAsync"/>, the batch command is considered used and becomes invalid for
/// further operations, preventing accidental reuse of the same batch.
/// </para>
/// </remarks>
/// <seealso cref="ISaveCommand{TInterface}"/>
/// <seealso cref="IBatchResult{TInterface}"/>
public interface IBatchCommand<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Adds a save command to the batch for atomic execution.
    /// </summary>
    /// <param name="saveCommand">The <see cref="ISaveCommand{TInterface}"/> to add to the batch.</param>
    /// <returns>The current <see cref="IBatchCommand{TInterface}"/> instance with the command added, enabling a fluent API for multiple additions.</returns>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="saveCommand"/> is not of the expected concrete implementation type.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the batch command has already been executed via <see cref="SaveAsync"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method adds a command to the current batch for later execution. The batch tracks each added
    /// command and will validate partition key consistency during execution to ensure atomicity.
    /// </para>
    /// <para>
    /// The method supports a fluent API pattern, allowing chained calls to add multiple commands:
    /// <code>
    /// var batch = commandProvider.Batch()
    ///     .Add(saveCommand1)
    ///     .Add(saveCommand2)
    ///     .Add(saveCommand3);
    /// </code>
    /// </para>
    /// <para>
    /// All commands in a batch must share the same partition key to ensure atomic execution.
    /// This constraint is validated at save time, not when adding commands to the batch.
    /// </para>
    /// </remarks>
    IBatchCommand<TInterface> Add(
        ISaveCommand<TInterface> saveCommand);

    /// <summary>
    /// Saves the batch of items to the backing data store as a single atomic transaction.
    /// </summary>
    /// <param name="requestContext">The <see cref="IRequestContext"/> that invoked this method, providing context for auditing and event tracking.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation if needed.</param>
    /// <returns>
    /// An array of <see cref="IBatchResult{TInterface}"/> objects, with one result for each command in the batch.
    /// Each result includes the HTTP status code indicating success or failure and, if successful, the saved item.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the batch command has already been executed.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method executes a batch of operations atomically, ensuring either all succeed or all fail together.
    /// The implementation performs these steps in sequence:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Validates all items in the batch before attempting to save</description>
    ///   </item>
    ///   <item>
    ///     <description>Acquires exclusive access to each item being modified</description>
    ///   </item>
    ///   <item>
    ///     <description>Verifies that all items share the same partition key (required for atomicity)</description>
    ///   </item>
    ///   <item>
    ///     <description>Executes the batch operation against the backing data store</description>
    ///   </item>
    ///   <item>
    ///     <description>Updates each command with the result and releases exclusive access</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The batch command becomes invalid after execution, preventing accidental reuse.
    /// </para>
    /// <para>
    /// If any item in the batch fails validation or has a different partition key, the entire
    /// batch operation fails. In case of failure, appropriate HTTP status codes are returned:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><c>400 Bad Request</c>: The specific item failed validation or has an invalid state</description>
    ///   </item>
    ///   <item>
    ///     <description><c>424 Failed Dependency</c>: The item itself was valid, but another item in the batch failed</description>
    ///   </item>
    /// </list>
    /// <para>
    /// If the batch contains no commands, an empty array is returned without interacting with the data store.
    /// </para>
    /// </remarks>
    /// <seealso cref="IBatchResult{TInterface}"/>
    /// <seealso cref="ValidateAsync"/>
    Task<IBatchResult<TInterface>[]> SaveAsync(
        IRequestContext requestContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates all items in the batch without saving them to the data store.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation if needed.</param>
    /// <returns>
    /// An array of <see cref="ValidationResult"/> objects, with one result for each command in the batch.
    /// Each result contains any validation errors found for that specific item.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled through <paramref name="cancellationToken"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method performs pre-flight validation of all items in the batch without persisting any changes
    /// to the data store. It's useful for checking if a batch operation would succeed before committing to it.
    /// </para>
    /// <para>
    /// Validation is performed in parallel for all items in the batch, with each item checked against both:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Base validation rules for <see cref="IBaseItem"/> properties</description>
    ///   </item>
    ///   <item>
    ///     <description>Domain-specific validation rules for the concrete item type</description>
    ///   </item>
    /// </list>
    /// <para>
    /// Unlike <see cref="SaveAsync"/>, this method does not validate partition key consistency
    /// across items in the batch. It only validates each item individually against its validation rules.
    /// </para>
    /// <para>
    /// The validation results can be inspected to determine if there are any issues that would
    /// prevent successful saving of the batch. Each <see cref="ValidationResult"/> contains
    /// detailed error information for the corresponding item.
    /// </para>
    /// </remarks>
    /// <seealso cref="SaveAsync"/>
    /// <seealso cref="FluentValidation.Results.ValidationResult"/>
    Task<ValidationResult[]> ValidateAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Implements functionality to save a batch of items atomically in the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items.</typeparam>
/// <remarks>
/// <para>
/// This class provides the concrete implementation of <see cref="IBatchCommand{TInterface}"/>, handling
/// the collection, validation, and atomic execution of multiple data operations as a single transaction.
/// </para>
/// <para>
/// The implementation uses several techniques to ensure data consistency and thread safety:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       A semaphore controls concurrent access, preventing race conditions during batch modification
///     </description>
///   </item>
///   <item>
///     <description>
///       Commands are validated before execution, catching validation errors early
///     </description>
///   </item>
///   <item>
///     <description>
///       Exclusive access to items is acquired and released in a coordinated manner
///     </description>
///   </item>
///   <item>
///     <description>
///       Partition key consistency is enforced during execution to ensure atomicity
///     </description>
///   </item>
///   <item>
///     <description>
///       After execution, the batch command is invalidated to prevent accidental reuse
///     </description>
///   </item>
/// </list>
/// <para>
/// The class handles error conditions carefully, ensuring all resources are properly released even
/// when operations fail or are canceled. Failed operations receive appropriate HTTP status codes
/// that distinguish between direct failures and dependency failures.
/// </para>
/// <para>
/// This implementation does not directly interact with the data store; instead, it coordinates
/// the batch operation and delegates the actual storage operation to the <see cref="SaveBatchAsyncDelegate{TInterface, TItem}"/>
/// provided during construction.
/// </para>
/// </remarks>
/// <param name="saveBatchAsyncDelegate">The delegate used to save the batch to the data store, implementing the storage-specific operations.</param>
/// <seealso cref="IBatchCommand{TInterface}"/>
/// <seealso cref="SaveBatchAsyncDelegate{TInterface, TItem}"/>
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
    /// The collection of save commands to be executed in this batch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This list maintains all the <see cref="SaveCommand{TInterface, TItem}"/> instances added to the batch
    /// via the <see cref="IBatchCommand{TInterface}.Add"/> method. The order of commands in this list
    /// corresponds to the order in which they were added.
    /// </para>
    /// <para>
    /// When <see cref="IBatchCommand{TInterface}.SaveAsync"/> is called, this field is set to <see langword="null"/>
    /// to invalidate the batch command and prevent accidental reuse.
    /// </para>
    /// <para>
    /// The collection is initialized as an empty list using C# 12's collection expression syntax.
    /// </para>
    /// </remarks>
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
    /// Builds batch results when one or more item acquisition tasks have failed.
    /// </summary>
    /// <param name="acquireTasks">The array of acquire tasks from which to build the batch results.</param>
    /// <returns>An array of batch results with appropriate failure status codes.</returns>
    /// <remarks>
    /// <para>
    /// This method handles the scenario where at least one of the tasks to acquire an item for
    /// batch processing has faulted. This can happen due to various reasons, such as an item
    /// having already been modified elsewhere or the save command being invalid.
    /// </para>
    /// <para>
    /// For each task, the method:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Releases any successfully acquired save commands to prevent resource leaks</description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Assigns appropriate HTTP status codes to indicate failure:
    ///       <list type="bullet">
    ///         <item>
    ///           <description>
    ///             <c>400 Bad Request</c> for tasks that faulted directly, indicating an issue with that specific item
    ///           </description>
    ///         </item>
    ///         <item>
    ///           <description>
    ///             <c>424 Failed Dependency</c> for tasks that would have succeeded but are failing due to other 
    ///             tasks in the batch failing, maintaining the atomic nature of the batch
    ///           </description>
    ///         </item>
    ///       </list>
    ///     </description>
    ///   </item>
    /// </list>
    /// <para>
    /// No attempt is made to save any items to the data store since the atomic batch operation
    /// cannot succeed when one or more items fail acquisition.
    /// </para>
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
    /// Executes the batch operation when all items have been successfully acquired.
    /// </summary>
    /// <param name="acquireTasks">The array of successful acquisition tasks containing the items to save.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation if needed.</param>
    /// <returns>An array of batch results with saved items and their status codes.</returns>
    /// <remarks>
    /// <para>
    /// This method handles the successful path of batch execution after all items have been
    /// successfully acquired for modification. It carries out these steps:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>Extracts the save requests from each successfully completed acquisition task</description>
    ///   </item>
    ///   <item>
    ///     <description>Determines the common partition key from the first item (all items share the same partition key)</description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Invokes the <see cref="SaveBatchAsyncDelegate{TInterface, TItem}"/> to perform the actual atomic 
    ///       save operation against the backing data store
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Processes the results from the save operation:
    ///       <list type="bullet">
    ///         <item>
    ///           <description>Updates each save command with its result if the operation succeeded</description>
    ///         </item>
    ///         <item>
    ///           <description>Creates appropriate batch results with status codes for each item</description>
    ///         </item>
    ///         <item>
    ///           <description>Releases all save commands to ensure resources are not leaked</description>
    ///         </item>
    ///       </list>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>Invalidates the batch command by setting the save commands list to null</description>
    ///   </item>
    /// </list>
    /// <para>
    /// The method determines overall success by checking if all individual save results have an HTTP status 
    /// code of OK (200). Only if all operations succeeded will the save commands be updated with their results.
    /// </para>
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
