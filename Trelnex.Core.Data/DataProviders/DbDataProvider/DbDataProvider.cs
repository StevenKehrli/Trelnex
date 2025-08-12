using System.Net;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base implementation for database-backed data providers using LinqToDB.
/// </summary>
/// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
/// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
/// <param name="dataOptions">LinqToDB connection and configuration options.</param>
/// <param name="typeName">Type name identifier used for filtering items.</param>
/// <param name="validator">Optional FluentValidation validator for domain-specific rules.</param>
/// <param name="commandOperations">Permitted CRUD operations. Defaults to Read-only if not specified.</param>
/// <param name="eventTimeToLive">Optional time-to-live for events in the table.</param>
/// <remarks>
/// Provides transactional data persistence with optimistic concurrency control and audit trail support.
/// Database-specific exception handling must be implemented in derived classes.
/// </remarks>
public abstract class DbDataProvider<TInterface, TItem>(
    DataOptions dataOptions,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null,
    int? eventTimeToLive = null)
    : DataProvider<TInterface, TItem>(typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Fields

    /// <summary>
    /// LinqToDB connection options for database operations.
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

    /// <inheritdoc/>
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
    /// Persists an item to the database and creates an associated audit event record.
    /// </summary>
    /// <param name="dataConnection">The active database connection within the current transaction.</param>
    /// <param name="request">The save request containing item data, save action, and audit event.</param>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    /// <returns>The saved item with any database-generated values like updated ETags.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the SaveAction is not recognized.</exception>
    /// <remarks>
    /// Performs INSERT for new items and UPDATE for modifications/deletions, followed by audit event insertion.
    /// The item is re-queried after save to ensure all database-generated values are included.
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
        var eventExpireAt = (eventTimeToLive is null)
            ? null as DateTimeOffset?
            : request.Event.CreatedDateTimeOffset.AddSeconds(eventTimeToLive.Value);

        var eventWithExpiration = new ItemEventWithExpiration(request.Event, eventExpireAt);

        dataConnection.Insert(eventWithExpiration);

        // Retrieve and return the saved item
        return dataConnection
            .GetTable<TItem>()
            .Where(i => i.Id == request.Item.Id && i.PartitionKey == request.Item.PartitionKey)
            .First();
    }

    /// <summary>
    /// Determines whether an exception originates from the database layer.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>True if the exception is database-specific; otherwise, false.</returns>
    /// <remarks>
    /// Override this method to identify database provider-specific exceptions that should be handled
    /// differently from general application exceptions.
    /// </remarks>
    protected abstract bool IsDatabaseException(Exception ex);

    /// <summary>
    /// Determines whether an exception represents an optimistic concurrency failure.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>True if the exception indicates a precondition failure (ETag mismatch); otherwise, false.</returns>
    /// <remarks>
    /// Override this method to identify database-specific exceptions that indicate optimistic concurrency
    /// violations, which should be mapped to HTTP 412 Precondition Failed status.
    /// </remarks>
    protected abstract bool IsPreconditionFailedException(Exception ex);

    /// <summary>
    /// Determines whether an exception represents a primary key constraint violation.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>True if the exception indicates a primary key violation; otherwise, false.</returns>
    /// <remarks>
    /// Override this method to identify database-specific exceptions that indicate primary key constraint
    /// violations, which should be mapped to HTTP 409 Conflict status.
    /// </remarks>
    protected abstract bool IsPrimaryKeyViolationException(Exception ex);

    #endregion
}