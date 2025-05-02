using System.Net;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base implementation of <see cref="ICommandProvider{TInterface}"/> that uses a database table as a backing store.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the item contract.</typeparam>
/// <typeparam name="TItem">The concrete item type that implements the interface.</typeparam>
/// <remarks>
/// This provider abstracts database operations for command handling, including CRUD operations,
/// transaction management, and error handling specific to database interactions.
/// </remarks>
public abstract class DbCommandProvider<TInterface, TItem> : CommandProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Fields

    /// <summary>
    /// The database connection options.
    /// </summary>
    private readonly DataOptions _dataOptions;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options for establishing database connections.</param>
    /// <param name="typeName">The type name used for filtering items in the database.</param>
    /// <param name="validator">The validator used to validate items before saving. Can be <see langword="null"/> if validation is not required.</param>
    /// <param name="commandOperations">The operations that this command provider supports. Can be <see langword="null"/> for default operations.</param>
    protected DbCommandProvider(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        : base(typeName, validator, commandOperations)
    {
        _dataOptions = dataOptions;
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Reads an item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The unique identifier of the item to read.</param>
    /// <param name="partitionKey">The partition key of the item for database sharding/partitioning.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous read operation. The task result contains the retrieved item,
    /// or <see langword="null"/> if the item doesn't exist.
    /// </returns>
    /// <exception cref="CommandException">
    /// Thrown when a database error occurs during the read operation.
    /// </exception>
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create the database connection
            using var dataConnection = new DataConnection(_dataOptions);

            // Query the item by id and partition key
            var item = dataConnection
                .GetTable<TItem>()
                .Where(i => i.Id == id && i.PartitionKey == partitionKey)
                .FirstOrDefault();

            return await Task.FromResult(item);
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            // Convert database exceptions to CommandExceptions with appropriate HTTP status code
            throw new CommandException(HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Saves a batch of items in the backing data store as an atomic transaction.
    /// </summary>
    /// <param name="partitionKey">The partition key shared by all items in the batch.</param>
    /// <param name="requests">The array of save requests, each containing an item and associated event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous batch save operation. The task result contains an array of
    /// <see cref="SaveResult{TInterface, TItem}"/> objects corresponding to each save request.
    /// </returns>
    /// <remarks>
    /// This method uses a transaction to ensure atomicity. If any request fails, the entire transaction is rolled back
    /// and subsequent requests are marked as failed dependencies.
    /// </remarks>
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        string partitionKey,
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Allocate the results array
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // Create a transaction scope with async flow enabled
        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        // Create the database connection
        using var dataConnection = new DataConnection(_dataOptions);

        // Process each save request in sequence
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            // Check if the previous item failed
            if (saveRequestIndex > 0 && saveResults[saveRequestIndex - 1].HttpStatusCode != HttpStatusCode.OK)
            {
                // Mark current request as failed dependency due to previous failure
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);

                continue;
            }

            var saveRequest = requests[saveRequestIndex];

            try
            {
                // Process the save request
                var saved = await SaveItemAsync(dataConnection, saveRequest, cancellationToken);

                // Record successful result
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                // Determine appropriate HTTP status code based on the exception type
                var httpStatusCode = HttpStatusCode.InternalServerError;

                if (IsPreconditionFailedException(ex))
                {
                    httpStatusCode = HttpStatusCode.PreconditionFailed;
                }

                if (IsPrimaryKeyViolationException(ex))
                {
                    httpStatusCode = HttpStatusCode.Conflict;
                }

                // Record failure result
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        httpStatusCode,
                        null);

                // Abort further processing
                break;
            }
        }

        if (saveRequestIndex == requests.Length)
        {
            // All requests succeeded, commit the transaction
            transactionScope.Complete();
        }
        else
        {
            // At least one request failed, mark all other requests as failed dependencies
            for (var saveResultIndex = 0; saveResultIndex < saveResults.Length; saveResultIndex++)
            {
                if (saveResultIndex == saveRequestIndex) continue;

                saveResults[saveResultIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);
            }
        }

        return await Task.FromResult(saveResults);
    }

    /// <summary>
    /// Creates an <see cref="IQueryable{TItem}"/> to query the items with default filtering applied.
    /// </summary>
    /// <returns>An <see cref="IQueryable{TItem}"/> with type name and soft delete filters applied.</returns>
    /// <remarks>
    /// This implementation returns an in-memory queryable with filters for TypeName and IsDeleted.
    /// The actual database query is constructed when <see cref="ExecuteQueryable"/> is called.
    /// </remarks>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Add typeName and isDeleted predicates
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Executes the provided queryable against the database and returns the results.
    /// </summary>
    /// <param name="queryable">The queryable expression to execute.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>An enumerable collection of items that match the query criteria.</returns>
    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Create the database connection
        using var dataConnection = new DataConnection(_dataOptions);

        // Create the database query from the queryable expression
        var queryableFromExpression = dataConnection
            .GetTable<TItem>()
            .Provider
            .CreateQuery<TItem>(queryable.Expression);

        // Return the results as an enumerable
        foreach (var item in queryableFromExpression.AsEnumerable())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Saves an item to the database based on the specified save action.
    /// </summary>
    /// <param name="dataConnection">The active database connection.</param>
    /// <param name="request">The save request containing the item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the saved item.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the save action is not recognized.</exception>
    protected virtual async Task<TItem> SaveItemAsync(
        DataConnection dataConnection,
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken)
    {
        switch (request.SaveAction)
        {
            case SaveAction.CREATED:
                await dataConnection.InsertAsync(obj: request.Item, token: cancellationToken);
                break;

            case SaveAction.UPDATED:
            case SaveAction.DELETED:
                await dataConnection.UpdateAsync(obj: request.Item, token: cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unrecognized SaveAction: {request.SaveAction}");
        }

        // Save the event record for auditing/history
        dataConnection.Insert(request.Event);

        // Retrieve and return the saved item to ensure we have the latest state
        return dataConnection
            .GetTable<TItem>()
            .Where(i => i.Id == request.Item.Id && i.PartitionKey == request.Item.PartitionKey)
            .First();
    }

    /// <summary>
    /// Determines if the given exception is a database-specific exception.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns><see langword="true"/> if the exception is a database-specific exception; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method is used to identify exceptions that originate from the database layer.
    /// Implementations should recognize the specific exception types thrown by their database provider.
    /// </remarks>
    protected abstract bool IsDatabaseException(Exception ex);

    /// <summary>
    /// Determines if the given exception indicates a precondition failure in the database.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception indicates a precondition failure (such as optimistic concurrency violation);
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This is typically used to detect optimistic concurrency conflicts, where the item
    /// has been modified by another process since it was last retrieved.
    /// </remarks>
    protected abstract bool IsPreconditionFailedException(Exception ex);

    /// <summary>
    /// Determines if the given exception indicates a primary key violation in the database.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception indicates a primary key constraint violation;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This is typically used to detect attempts to insert items with keys that already exist
    /// in the database, which would violate uniqueness constraints.
    /// </remarks>
    protected abstract bool IsPrimaryKeyViolationException(Exception ex);

    #endregion
}