using System.Net;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;

namespace Trelnex.Core.Data;

/// <summary>
/// Abstract base implementation of <see cref="ICommandProvider{TInterface}"/> that uses a relational database
/// as a backing store for persisting and querying items.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the item contract.</typeparam>
/// <typeparam name="TItem">The concrete item type that implements the interface.</typeparam>
/// <remarks>
/// <para>
/// This provider implements database-specific operations for command handling, functioning as a bridge between
/// the command pattern abstraction and actual database operations. It provides implementations for:
/// </para>
/// <list type="bullet">
///   <item><description>CRUD operations against database tables</description></item>
///   <item><description>Transaction management for atomic batch operations</description></item>
///   <item><description>Error handling and translation of database-specific exceptions</description></item>
///   <item><description>LINQ query translation to database queries</description></item>
/// </list>
/// <para>
/// The provider uses the LINQ to DB library for database access, which provides a LINQ interface for
/// working with various database engines (SQL Server, PostgreSQL, SQLite, etc.). The specific database
/// connection details are encapsulated in the <see cref="DataOptions"/> provided to the constructor.
/// </para>
/// <para>
/// This class is abstract because database-specific aspects such as exception handling must be implemented
/// by derived classes to support different database engines. Concrete implementations of this class should:
/// </para>
/// <list type="bullet">
///   <item><description>Implement database-specific exception analysis methods</description></item>
///   <item><description>Optionally override the item saving logic if needed</description></item>
///   <item><description>Configure appropriate database connection details</description></item>
/// </list>
/// <para>
/// The provider implements soft delete by filtering out items with <c>IsDeleted=true</c> in queries,
/// and maintains an audit trail by recording <see cref="ItemEvent{TItem}"/> records alongside entity changes.
/// </para>
/// </remarks>
/// <seealso cref="CommandProvider{TInterface, TItem}"/>
/// <seealso cref="DbCommandProviderFactory"/>
/// <seealso cref="DataConnection"/>
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
    /// <param name="dataOptions">The data connection options encapsulating database connection details and configuration.</param>
    /// <param name="typeName">The type name used for filtering items in the database and stored in the <see cref="BaseItem.TypeName"/> property.</param>
    /// <param name="validator">Optional domain-specific validator for items of type <typeparamref name="TItem"/>. Can be <see langword="null"/> if only basic property validation is required.</param>
    /// <param name="commandOperations">The operations that this command provider supports (Update/Delete). Defaults to <see cref="CommandOperations.Update"/> if <see langword="null"/>.</param>
    /// <remarks>
    /// <para>
    /// This constructor configures the database command provider with the specified options and passes
    /// core configuration to the base <see cref="CommandProvider{TInterface, TItem}"/> class.
    /// </para>
    /// <para>
    /// The <paramref name="dataOptions"/> parameter encapsulates the database connection details, including:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Connection string or connection information</description></item>
    ///   <item><description>Database provider configuration</description></item>
    ///   <item><description>Command timeout settings</description></item>
    ///   <item><description>Transaction isolation level defaults</description></item>
    /// </list>
    /// <para>
    /// The <paramref name="typeName"/> parameter must follow naming conventions (lowercase letters and hyphens,
    /// starting and ending with a letter) and is stored in each item's <see cref="BaseItem.TypeName"/> property.
    /// It serves as a discriminator in database tables that store multiple entity types.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the <paramref name="typeName"/> does not follow naming conventions or is a reserved name.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dataOptions"/> is <see langword="null"/>.
    /// </exception>
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
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Pre-allocate the results array to match the number of requests
        // This array will hold the outcome of each save operation
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // Create a transaction scope to ensure atomicity of the batch operation
        // We use AsyncFlowOption.Enabled to allow async operations within the transaction
        // If any operation fails, the entire transaction will be rolled back automatically
        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        // Create a single database connection to be reused for all operations
        // This ensures all operations use the same transaction and improves performance
        using var dataConnection = new DataConnection(_dataOptions);

        // Process each save request in sequence, tracking the current index
        // We need to track the index to correctly handle partial failures
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            // Fast-fail pattern: if any previous operation failed, don't attempt further saves
            // This is part of the atomicity guarantee - either all succeed or all fail
            // FailedDependency (424) indicates that this item would have been saved
            // but another item in the batch failed, so this one is skipped
            if (saveRequestIndex > 0 && saveResults[saveRequestIndex - 1].HttpStatusCode != HttpStatusCode.OK)
            {
                // Mark this request as a dependency failure and skip actual processing
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);

                continue;
            }

            var saveRequest = requests[saveRequestIndex];

            try
            {
                // Perform the actual database operation for this request
                // This may be an insert for creates or update for updates/deletes
                // The SaveItemAsync method handles the different cases based on SaveAction
                var saved = await SaveItemAsync(dataConnection, saveRequest, cancellationToken);

                // Record this operation as successful (HTTP 200 OK) and store the saved item
                // The saved item includes any database-generated values or timestamps
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (IsDatabaseException(ex))
            {
                // Handle database-specific exceptions by mapping them to appropriate HTTP status codes
                // Default to 500 Internal Server Error for unexpected database errors
                var httpStatusCode = HttpStatusCode.InternalServerError;

                // Map optimistic concurrency failures to 412 Precondition Failed
                // This occurs if the item has been modified by another operation
                // since it was originally read (detected via ETag/timestamp comparison)
                if (IsPreconditionFailedException(ex))
                {
                    httpStatusCode = HttpStatusCode.PreconditionFailed;
                }

                // Map primary key violations to 409 Conflict
                // This occurs when trying to insert an item with an ID that already exists
                if (IsPrimaryKeyViolationException(ex))
                {
                    httpStatusCode = HttpStatusCode.Conflict;
                }

                // Record this specific operation's failure with the appropriate status code
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        httpStatusCode,
                        null);

                // Stop processing further requests since the transaction will be rolled back
                // We break out of the loop here but still need to update remaining results
                break;
            }
        }

        // Check if all requests were processed successfully
        if (saveRequestIndex == requests.Length)
        {
            // All operations succeeded - commit the transaction to make changes permanent
            // This is a critical step - without this call, all changes would be rolled back
            transactionScope.Complete();
        }
        else
        {
            // At least one operation failed - the transaction will automatically roll back
            // We need to mark the status of any remaining unprocessed items or items
            // that weren't yet marked as dependency failures
            for (var saveResultIndex = 0; saveResultIndex < saveResults.Length; saveResultIndex++)
            {
                // Skip the item that directly failed - it already has the correct error code
                if (saveResultIndex == saveRequestIndex) continue;

                // Mark all other items as dependency failures (HTTP 424)
                // This includes both items we skipped earlier and items we never got to
                saveResults[saveResultIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);
            }
        }

        // Return the array of results that indicates the outcome of each save operation
        // Since we've completely built the results array, we can simply wrap it in a completed task
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
        // Create a new database connection for this query execution
        // Each query gets its own connection since it's not part of a transaction
        // The connection is disposed when the method completes via 'using'
        using var dataConnection = new DataConnection(_dataOptions);

        // Transform the abstract queryable into a database-specific query
        // This is a critical step that takes the expression tree from the abstract queryable
        // and creates a concrete database query that the LINQ to DB provider can execute
        // The steps are:
        // 1. Get the TItem table from the database connection
        // 2. Get the query provider from the table reference
        // 3. Create a new queryable using our expression tree but with the database provider
        var queryableFromExpression = dataConnection
            .GetTable<TItem>()
            .Provider
            .CreateQuery<TItem>(queryable.Expression);

        // Execute the query and stream results using yield return
        // This creates a streaming iterator that fetches records as they're consumed
        // Benefits:
        // - Memory efficient for large result sets
        // - Supports client-side filtering after database results are returned
        // - Allows LINQ method chaining on the returned sequence
        // Note: The entire result set is still retrieved from the database at once,
        // the streaming is only for the client-side processing
        foreach (var item in queryableFromExpression.AsEnumerable())
        {
            yield return item;
        }
    }

    /// <summary>
    /// Saves an item to the database based on the specified save action and creates an associated audit event record.
    /// </summary>
    /// <param name="dataConnection">The active database connection within the current transaction scope.</param>
    /// <param name="request">The save request containing the item to persist and its associated audit event.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the saved item with any database-generated values.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the save action is not a recognized value from the <see cref="SaveAction"/> enum.</exception>
    /// <remarks>
    /// <para>
    /// This method performs the actual database operations to persist the item and its associated audit event.
    /// It uses different database operations based on the save action:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="SaveAction.Created"/>: Performs an INSERT operation to add a new item to the database.</description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="SaveAction.Updated"/> or <see cref="SaveAction.Deleted"/>: Performs an UPDATE operation to modify an existing item.</description>
    ///   </item>
    /// </list>
    /// <para>
    /// After saving the item, it creates an audit trail by inserting an <see cref="ItemEvent{TItem}"/> record
    /// that captures the save action, the item's identity, and any property changes. This provides a historical
    /// record of all modifications to the item for tracking and auditing purposes.
    /// </para>
    /// <para>
    /// The method returns the saved item after retrieving it from the database to ensure that any
    /// database-generated values (like timestamps or triggers) are included in the returned object.
    /// </para>
    /// <para>
    /// This method is virtual to allow derived classes to customize the save behavior for specific
    /// database engines or to implement additional functionality such as caching, logging, or handling
    /// database-specific features.
    /// </para>
    /// </remarks>
    /// <seealso cref="SaveBatchAsync"/>
    /// <seealso cref="SaveAction"/>
    /// <seealso cref="ItemEvent{TItem}"/>
    protected virtual async Task<TItem> SaveItemAsync(
        DataConnection dataConnection,
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken)
    {
        // Determine the appropriate database operation based on the save action
        // Each action corresponds to a different database operation pattern:
        switch (request.SaveAction)
        {
            case SaveAction.CREATED:
                // For new items, use INSERT to add the record to the database
                // This runs an "INSERT INTO" SQL statement with all mapped item properties
                // If the database has auto-generated columns, they'll be populated after this call
                await dataConnection.InsertAsync(obj: request.Item, token: cancellationToken);
                break;

            case SaveAction.UPDATED:
            case SaveAction.DELETED:
                // For both updates and soft deletes, use UPDATE to modify the existing record
                // Note that deletes are implemented as updates setting IsDeleted=true (soft delete)
                // This runs an "UPDATE" SQL statement for the item's primary key
                await dataConnection.UpdateAsync(obj: request.Item, token: cancellationToken);
                break;

            default:
                // Safety check for unrecognized enum values
                // This handles future enum values that might be added but not implemented here
                throw new InvalidOperationException($"Unrecognized SaveAction: {request.SaveAction}");
        }

        // Save the event record for auditing/history tracking
        // This creates a separate record in the event table that records:
        // - When the change happened and by whom (from request context)
        // - What type of change it was (create/update/delete)
        // - Which properties changed and their old/new values
        // Note: This is deliberately not async for performance reasons
        // since audit logs are secondary to the main data operation
        dataConnection.Insert(request.Event);

        // Retrieve and return the saved item from the database
        // This ensures we have the latest state including:
        // - Any database-generated values (timestamps, calculated fields)
        // - Any changes made by triggers or stored procedures
        // - The exact representation that was committed to the database
        return dataConnection
            .GetTable<TItem>()
            .Where(i => i.Id == request.Item.Id && i.PartitionKey == request.Item.PartitionKey)
            .First();
    }

    /// <summary>
    /// Determines if a given exception is a database-specific exception that should be handled by the database provider.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception is a database-specific exception that should be handled
    /// by the database provider; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is used to identify exceptions that originate from the database layer so they can
    /// be properly handled and translated to appropriate business layer exceptions or HTTP status codes.
    /// </para>
    /// <para>
    /// Each database technology (SQL Server, PostgreSQL, SQLite, etc.) has its own set of exception types
    /// and error codes. Implementations of this method should recognize the specific exception types and
    /// patterns thrown by their database provider to properly classify them.
    /// </para>
    /// <para>
    /// Examples of database-specific exceptions include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Connection failures</description></item>
    ///   <item><description>Constraint violations (primary key, foreign key, unique)</description></item>
    ///   <item><description>Timeout exceptions</description></item>
    ///   <item><description>Deadlock exceptions</description></item>
    ///   <item><description>Schema validation errors</description></item>
    /// </list>
    /// <para>
    /// When this method returns <see langword="true"/>, the exception is handled by the database provider's
    /// error handling logic. When it returns <see langword="false"/>, the exception is propagated to the caller.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // SQL Server implementation example
    /// protected override bool IsDatabaseException(Exception ex)
    /// {
    ///     return ex is SqlException ||
    ///            ex is SqlTypeException ||
    ///            (ex is InvalidOperationException && ex.Message.Contains("SQL Server"));
    /// }
    /// </code>
    /// </example>
    protected abstract bool IsDatabaseException(Exception ex);

    /// <summary>
    /// Determines if a given exception indicates a precondition failure in the database, such as
    /// an optimistic concurrency violation.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception indicates a precondition failure (such as optimistic
    /// concurrency violation); otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is used to identify exceptions that represent precondition failures in the database,
    /// particularly optimistic concurrency violations. These occur when an attempt is made to update a
    /// record that has been modified by another process since it was retrieved.
    /// </para>
    /// <para>
    /// In many database systems, optimistic concurrency is implemented using row version or timestamp columns.
    /// When an update is attempted with an outdated version, the database rejects the change, resulting in
    /// an exception that should be classified as a precondition failure.
    /// </para>
    /// <para>
    /// When this method returns <see langword="true"/>, the exception is typically mapped to an HTTP 412
    /// (Precondition Failed) status code, indicating to clients that they need to refresh their data
    /// and attempt the operation again with the current version.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // SQL Server implementation example
    /// protected override bool IsPreconditionFailedException(Exception ex)
    /// {
    ///     if (ex is SqlException sqlEx)
    ///     {
    ///         // Check for error numbers that indicate optimistic concurrency failures
    ///         return sqlEx.Number == 3960 || // Snapshot isolation transaction aborted due to update conflict
    ///                sqlEx.Number == 8645;   // Row version has changed
    ///     }
    ///     return false;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="HttpStatusCode.PreconditionFailed"/>
    protected abstract bool IsPreconditionFailedException(Exception ex);

    /// <summary>
    /// Determines if a given exception indicates a primary key violation in the database, such as
    /// attempting to insert a record with a key that already exists.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>
    /// <see langword="true"/> if the exception indicates a primary key constraint violation;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is used to identify exceptions that represent primary key constraint violations in the database.
    /// These occur when an attempt is made to insert a record with a key (ID and partition key combination)
    /// that already exists in the database.
    /// </para>
    /// <para>
    /// Primary key violations are common in systems that use client-generated IDs, especially when:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Multiple clients attempt to create items with the same ID</description></item>
    ///   <item><description>A client attempts to create an item without checking if it already exists</description></item>
    ///   <item><description>IDs are generated using algorithms that can potentially produce duplicates</description></item>
    /// </list>
    /// <para>
    /// When this method returns <see langword="true"/>, the exception is typically mapped to an HTTP 409
    /// (Conflict) status code, indicating to clients that the resource they're trying to create already exists.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // SQL Server implementation example
    /// protected override bool IsPrimaryKeyViolationException(Exception ex)
    /// {
    ///     if (ex is SqlException sqlEx)
    ///     {
    ///         // Check for error number that indicates primary key violation
    ///         return sqlEx.Number == 2627 || // Violation of primary key constraint
    ///                sqlEx.Number == 2601;   // Violation of unique index
    ///     }
    ///     return false;
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="HttpStatusCode.Conflict"/>
    protected abstract bool IsPrimaryKeyViolationException(Exception ex);

    #endregion
}