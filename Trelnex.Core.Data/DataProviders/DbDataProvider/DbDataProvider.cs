using System.Net;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base class for database-backed data providers using LinqToDB.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
/// <param name="dataOptions">LinqToDB connection and configuration options.</param>
/// <param name="typeName">Type name identifier for filtering items.</param>
/// <param name="itemValidator">Optional validator for domain-specific rules.</param>
/// <param name="commandOperations">Allowed CRUD operations, defaults to Read-only.</param>
/// <param name="eventTimeToLive">Optional time-to-live for events in seconds.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
public abstract class DbDataProvider<TItem>(
    string typeName,
    DataOptions dataOptions,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null,
    int? eventTimeToLive = null,
    ILogger? logger = null)
    : DataProvider<TItem>(
        typeName: typeName,
        itemValidator: itemValidator,
        commandOperations: commandOperations,
        logger: logger)
    where TItem : BaseItem, new()
{
    #region Private Fields

    // LinqToDB configuration for database connections
    private readonly DataOptions _dataOptions = dataOptions;

    #endregion

    #region Protected Methods

    /// <inheritdoc/>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Return empty queryable with filters - actual data substitution happens in ExecuteQueryableAsync
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
        // Create database connection
        using var dataConnection = new DataConnection(_dataOptions);

        // Replace empty queryable with actual database table query
        var queryableFromExpression = dataConnection
            .GetTable<TItem>()
            .Provider
            .CreateQuery<TItem>(queryable.Expression);

        // Stream results from database
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
            // Create database connection
            using var dataConnection = new DataConnection(_dataOptions);

            // Query for specific item by primary key
            var item = dataConnection
                .GetTable<TItem>()
                .Where(i => i.Id == id && i.PartitionKey == partitionKey && i.TypeName == TypeName)
                .FirstOrDefault();

            return item;
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            // Convert database errors to command exceptions
            throw new CommandException(HttpStatusCode.InternalServerError, ex.Message);
        }
    }
#pragma warning restore CS1998

    /// <inheritdoc/>
    [TraceMethod]
    protected override async Task<SaveResult<TItem>[]> SaveBatchAsync(
        SaveRequest<TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Initialize results array
        var saveResults = new SaveResult<TItem>[requests.Length];

        // Use transaction scope for atomic batch operations
        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        // Single connection for all operations in the batch
        using var dataConnection = new DataConnection(_dataOptions);

        // Process requests sequentially
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            // Skip processing if previous request failed
            if (saveRequestIndex > 0 && saveResults[saveRequestIndex - 1].HttpStatusCode != HttpStatusCode.OK)
            {
                saveResults[saveRequestIndex] =
                    new SaveResult<TItem>(
                        HttpStatusCode.FailedDependency,
                        null);

                continue;
            }

            var saveRequest = requests[saveRequestIndex];

            try
            {
                // Save item to database
                var saved = await SaveItemAsync(dataConnection, saveRequest, cancellationToken);

                // Record success
                saveResults[saveRequestIndex] =
                    new SaveResult<TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                // Handle database-specific errors
                var httpStatusCode = HttpStatusCode.InternalServerError;

                // Map specific error types to appropriate HTTP status codes
                if (IsPreconditionFailedException(ex))
                {
                    httpStatusCode = HttpStatusCode.PreconditionFailed;
                }

                if (IsPrimaryKeyViolationException(ex))
                {
                    httpStatusCode = HttpStatusCode.Conflict;
                }

                // Record failure
                saveResults[saveRequestIndex] =
                    new SaveResult<TItem>(
                        httpStatusCode,
                        null);

                // Stop processing on first failure
                break;
            }
        }

        // Commit or rollback based on success
        if (saveRequestIndex == requests.Length)
        {
            transactionScope.Complete();
        }
        else
        {
            // Mark remaining requests as dependency failures
            for (var saveResultIndex = 0; saveResultIndex < saveResults.Length; saveResultIndex++)
            {
                if (saveResultIndex == saveRequestIndex) continue;

                saveResults[saveResultIndex] =
                    new SaveResult<TItem>(
                        HttpStatusCode.FailedDependency,
                        null);
            }
        }

        return saveResults;
    }

    /// <summary>
    /// Saves an item to the database and records an audit event.
    /// </summary>
    /// <param name="dataConnection">Active database connection within transaction.</param>
    /// <param name="request">Save request containing item and event data.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The saved item with database-generated values.</returns>
    /// <exception cref="InvalidOperationException">Thrown for unrecognized save actions.</exception>
    protected virtual async Task<TItem> SaveItemAsync(
        DataConnection dataConnection,
        SaveRequest<TItem> request,
        CancellationToken cancellationToken)
    {
        // Execute appropriate database operation based on save action
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

        // Calculate event expiration time if TTL is configured
        var eventExpireAt = (eventTimeToLive is null)
            ? null as DateTimeOffset?
            : request.Event.CreatedDateTimeOffset.AddSeconds(eventTimeToLive.Value);

        // Insert audit event with optional expiration
        var eventWithExpiration = new ItemEventWithExpiration(request.Event, eventExpireAt);
        dataConnection.Insert(eventWithExpiration);

        // Return saved item with any database-generated values
        return dataConnection
            .GetTable<TItem>()
            .Where(i => i.Id == request.Item.Id && i.PartitionKey == request.Item.PartitionKey)
            .First();
    }

    /// <summary>
    /// Determines if an exception originates from the database layer.
    /// </summary>
    /// <param name="ex">Exception to analyze.</param>
    /// <returns>True if the exception is database-specific.</returns>
    protected abstract bool IsDatabaseException(Exception ex);

    /// <summary>
    /// Determines if an exception represents an optimistic concurrency failure.
    /// </summary>
    /// <param name="ex">Exception to analyze.</param>
    /// <returns>True if the exception indicates ETag mismatch.</returns>
    protected abstract bool IsPreconditionFailedException(Exception ex);

    /// <summary>
    /// Determines if an exception represents a primary key constraint violation.
    /// </summary>
    /// <param name="ex">Exception to analyze.</param>
    /// <returns>True if the exception indicates primary key violation.</returns>
    protected abstract bool IsPrimaryKeyViolationException(Exception ex);

    #endregion
}