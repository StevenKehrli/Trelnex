using System.Net;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base implementation of <see cref="ICommandProvider{TInterface}"/> that uses a relational database.
/// </summary>
/// <param name="dataOptions">The data connection options.</param>
/// <param name="typeName">The type name used for filtering items.</param>
/// <param name="validator">Optional validator for items before they are saved.</param>
/// <param name="commandOperations">The operations that this command provider supports (Read/Create/Update/Delete). Defaults to <see cref="CommandOperations.Read"/> if <see langword="null"/>.</param>
/// <param name="encryptionService">Optional encryption service for encrypting sensitive data.</param>
public abstract class DbCommandProvider<TInterface, TItem>(
    DataOptions dataOptions,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null)
    : CommandProvider<TInterface, TItem>(typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Fields

    /// <summary>
    /// The database connection options.
    /// </summary>
    private readonly DataOptions _dataOptions = dataOptions;

    #endregion

    #region Protected Methods

    /// <inheritdoc/>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Add typeName and isDeleted filters
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <inheritdoc/>
#pragma warning disable CS1998, CS8425
    [TraceMethod]
    protected override async IAsyncEnumerable<TItem> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Get database connection
        using var dataConnection = new DataConnection(_dataOptions);

        // Transform queryable into database-specific query
        var queryableFromExpression = dataConnection
            .GetTable<TItem>()
            .Provider
            .CreateQuery<TItem>(queryable.Expression);

        // Execute query and stream results
        foreach (var item in queryableFromExpression)
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;
        }
    }
#pragma warning restore CS1998, CS8425

    /// <inheritdoc/>
#pragma warning disable CS1998
    [TraceMethod]
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get database connection
            using var dataConnection = new DataConnection(_dataOptions);

            // Query item by id and partition key
            var item = dataConnection
                .GetTable<TItem>()
                .Where(i => i.Id == id && i.PartitionKey == partitionKey && i.TypeName == TypeName)
                .FirstOrDefault();

            return item;
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            // Convert database exceptions to CommandExceptions
            throw new CommandException(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
#pragma warning restore CS1998

    /// <summary>
    /// Saves a batch of items in the backing data store as an atomic transaction.
    /// </summary>
    /// <param name="requests">The array of save requests.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A task that represents the asynchronous batch save operation.
    /// </returns>
    /// <remarks>
    /// Uses a transaction to ensure atomicity.
    /// </remarks>
    [TraceMethod]
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Pre-allocate results array
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // Create transaction scope for atomicity
        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        // Create single database connection for all operations
        using var dataConnection = new DataConnection(_dataOptions);

        // Process each save request in sequence
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            // Fast-fail: skip saves if previous operation failed
            if (saveRequestIndex > 0 && saveResults[saveRequestIndex - 1].HttpStatusCode != HttpStatusCode.OK)
            {
                // Mark as dependency failure and skip
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);

                continue;
            }

            var saveRequest = requests[saveRequestIndex];

            try
            {
                // Perform database operation
                var saved = await SaveItemAsync(dataConnection, saveRequest, cancellationToken);

                // Record successful operation
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                // Handle database-specific exceptions
                var httpStatusCode = HttpStatusCode.InternalServerError;

                // Map optimistic concurrency failures
                if (IsPreconditionFailedException(ex))
                {
                    httpStatusCode = HttpStatusCode.PreconditionFailed;
                }

                // Map primary key violations
                if (IsPrimaryKeyViolationException(ex))
                {
                    httpStatusCode = HttpStatusCode.Conflict;
                }

                // Record operation failure
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        httpStatusCode,
                        null);

                // Stop processing further requests
                break;
            }
        }

        // Check if all requests succeeded
        if (saveRequestIndex == requests.Length)
        {
            // Commit transaction
            transactionScope.Complete();
        }
        else
        {
            // Rollback transaction and mark failures
            for (var saveResultIndex = 0; saveResultIndex < saveResults.Length; saveResultIndex++)
            {
                // Skip the item that failed
                if (saveResultIndex == saveRequestIndex) continue;

                // Mark other items as dependency failures
                saveResults[saveResultIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);
            }
        }

        // Return results
        return saveResults;
    }

    /// <summary>
    /// Saves an item to the database and creates an associated audit event record.
    /// </summary>
    /// <param name="dataConnection">The active database connection.</param>
    /// <param name="request">The save request.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The saved item with any database-generated values.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the save action is not recognized.</exception>
    /// <remarks>
    /// Performs the actual database operations to persist the item and its associated audit event.
    /// </remarks>
    protected virtual async Task<TItem> SaveItemAsync(
        DataConnection dataConnection,
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken)
    {
        // Determine database operation based on save action
        switch (request.SaveAction)
        {
            case SaveAction.CREATED:
                // Use INSERT for new items
                await dataConnection.InsertAsync(obj: request.Item, token: cancellationToken);
                break;

            case SaveAction.UPDATED:
            case SaveAction.DELETED:
                // Use UPDATE for updates and deletes
                await dataConnection.UpdateAsync(obj: request.Item, token: cancellationToken);
                break;

            default:
                // Handle unrecognized enum values
                throw new InvalidOperationException($"Unrecognized SaveAction: {request.SaveAction}");
        }

        // Save event record for auditing
        dataConnection.Insert(request.Event);

        // Retrieve and return the saved item
        return dataConnection
            .GetTable<TItem>()
            .Where(i => i.Id == request.Item.Id && i.PartitionKey == request.Item.PartitionKey)
            .First();
    }

    /// <summary>
    /// Determines if a given exception is a database-specific exception.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception is a database-specific exception; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Used to identify exceptions that originate from the database layer.
    /// </remarks>
    protected abstract bool IsDatabaseException(Exception ex);

    /// <summary>
    /// Determines if a given exception indicates a precondition failure.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception indicates a precondition failure; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Used to identify exceptions that represent precondition failures in the database.
    /// </remarks>
    protected abstract bool IsPreconditionFailedException(Exception ex);

    /// <summary>
    /// Determines if a given exception indicates a primary key violation.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception indicates a primary key constraint violation; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Used to identify exceptions that represent primary key constraint violations in the database.
    /// </remarks>
    protected abstract bool IsPrimaryKeyViolationException(Exception ex);

    #endregion
}