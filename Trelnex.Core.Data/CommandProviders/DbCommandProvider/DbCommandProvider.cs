using System.Net;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base implementation of <see cref="ICommandProvider{TInterface}"/> that uses a database table as a backing store.
/// </summary>
public abstract class DbCommandProvider<TInterface, TItem> : CommandProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    private readonly DataOptions _dataOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="dataOptions">The data connection options.</param>
    /// <param name="typeName">The type name.</param>
    /// <param name="validator">The validator.</param>
    /// <param name="commandOperations">The command operations.</param>
    protected DbCommandProvider(
        DataOptions dataOptions,
        string typeName,
        IValidator<TItem>? validator = null,
        CommandOperations? commandOperations = null)
        : base(typeName, validator, commandOperations)
    {
        _dataOptions = dataOptions;
    }

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The item that was read.</returns>
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // create the connection
            using var dataConnection = new DataConnection(_dataOptions);

            // get the item
            var item = dataConnection
                .GetTable<TItem>()
                .Where(i => i.Id == id && i.PartitionKey == partitionKey)
                .FirstOrDefault();

            return await Task.FromResult(item);
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            var httpStatusCode = HttpStatusCode.InternalServerError;

            throw new CommandException(httpStatusCode, ex.Message);
        }
    }

    /// <summary>
    /// Saves a batch of items in the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="partitionKey">The partition key of the batch.</param>
    /// <param name="requests">The batch of save requests with item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The results of the batch operation.</returns>
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        string partitionKey,
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // allocate the results
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // create the transaction
        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        // create the connection
        using var dataConnection = new DataConnection(_dataOptions);

        // enumerate each item
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            // check for if previous item failed
            if (saveRequestIndex > 0 && saveResults[saveRequestIndex - 1].HttpStatusCode != HttpStatusCode.OK)
            {
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);

                continue;
            }

            var saveRequest = requests[saveRequestIndex];

            try
            {
                // save the item
                var saved = await SaveItemAsync(dataConnection, saveRequest, cancellationToken);

                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                var httpStatusCode = HttpStatusCode.InternalServerError;

                if (IsPreconditionFailedException(ex))
                {
                    httpStatusCode = HttpStatusCode.PreconditionFailed;
                }

                if (IsPrimaryKeyViolationException(ex))
                {
                    httpStatusCode = HttpStatusCode.Conflict;
                }

                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        httpStatusCode,
                        null);

                // abort any further processing
                break;
            }
        }

        if (saveRequestIndex == requests.Length)
        {
            // the batch completed successfully, complete the transaction
            transactionScope.Complete();
        }
        else
        {
            // a save request failed
            // update all other results to failed dependency
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
    /// Create the <see cref="IQueryable{TItem}"/> to query the items.
    /// </summary>
    /// <returns></returns>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // add typeName and isDeleted predicates
        // the lambda parameter i is an item of TInterface type
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Execute the query specified by the <see cref="IQueryable{TItem}"/> and return the results as an enumerable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The <see cref="IEnumerable{TInterface}"/>.</returns>
    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // create the connection
        using var dataConnection = new DataConnection(_dataOptions);

        // create the query from the table and the queryable expression
        var queryableFromExpression = dataConnection
            .GetTable<TItem>()
            .Provider
            .CreateQuery<TItem>(queryable.Expression);

        foreach (var item in queryableFromExpression.AsEnumerable())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Save the item to the backing store.
    /// </summary>
    /// <param name="dataConnection">The data connection.</param>
    /// <param name="request">The save request with item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The result of the save operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="SaveAction"/> is not recognized.</exception>
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

        dataConnection.Insert(request.Event);

        // get the saved item
        return dataConnection
            .GetTable<TItem>()
            .Where(i => i.Id == request.Item.Id && i.PartitionKey == request.Item.PartitionKey)
            .First();
    }
    
    /// <summary>
    /// Determines if the given exception is a database-specific exception.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception is a database exception; otherwise, false.</returns>
    protected abstract bool IsDatabaseException(Exception ex);
    
    /// <summary>
    /// Determines if the given exception indicates a precondition failure in the database.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a precondition failure; otherwise, false.</returns>
    protected abstract bool IsPreconditionFailedException(Exception ex);
    
    /// <summary>
    /// Determines if the given exception indicates a primary key violation in the database.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a primary key violation; otherwise, false.</returns>
    protected abstract bool IsPrimaryKeyViolationException(Exception ex);
}