using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Transactions;
using FluentValidation;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.Data.SqlClient;

namespace Trelnex.Core.Data;

/// <summary>
/// An implementation of <see cref="ICommandProvider{TInterface}"/> that uses a SQL table as a backing store.
/// </summary>
internal partial class SqlCommandProvider<TInterface, TItem>(
    DataOptions dataOptions,
    string typeName,
    AbstractValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null)
    : CommandProvider<TInterface, TItem>(typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
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
            using var dataConnection = new DataConnection(dataOptions);

            // get the item
            var item = dataConnection
                .GetTable<TItem>()
                .Where(i => i.Id == id && i.PartitionKey == partitionKey)
                .FirstOrDefault();

            return await Task.FromResult(item);
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        catch (CosmosException ce)
        {
            throw new CommandException(ce.StatusCode, ce.Message);
        }
    }

    /// <summary>
    /// Saves a item in the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="request">The save request with item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The item that was saved.</returns>
    protected override async Task<TItem> SaveItemAsync(
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken = default)
    {
        // create the transaction
        using var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        // create the connection
        using var dataConnection = new DataConnection(dataOptions);

        try
        {
            // save the item
            var saved = await SaveItemAsync(dataConnection, request, cancellationToken);

            // commit the transaction
            transactionScope.Complete();

            return await Task.FromResult(saved);
        }
        catch (SqlException se)
        {
            if (PreconditionFailedRegex().IsMatch(se.Message))
            {
                throw new CommandException(HttpStatusCode.PreconditionFailed);
            }

            if (PrimaryKeyViolationRegex().IsMatch(se.Message))
            {
                throw new CommandException(HttpStatusCode.Conflict);
            }

            throw new CommandException(HttpStatusCode.InternalServerError, se.Message);
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
        using var dataConnection = new DataConnection(dataOptions);

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
            catch (Exception ex) when (ex is CommandException || ex is InvalidOperationException || ex is SqlException)
            {
                // set the result to the exception status code
                var httpStatusCode = ex is CommandException ce
                    ? ce.HttpStatusCode
                    : HttpStatusCode.InternalServerError;

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
    /// Create an instance of the <see cref="IQueryCommand{Interface}"/>.
    /// </summary>
    /// <param name="expressionConverter">The <see cref="ExpressionConverter{TInterface,TItem}"/> to convert an expression using a TInterface to an expression using a TItem.</param>
    /// <param name="convertToQueryResult">The method to convert a TItem to a <see cref="IQueryResult{TInterface}"/>.</param>
    /// <returns>The <see cref="IQueryCommand{Interface}"/>.</returns>
    protected override IQueryCommand<TInterface> CreateQueryCommand(
        ExpressionConverter<TInterface, TItem> expressionConverter,
        Func<TItem, IQueryResult<TInterface>> convertToQueryResult)
    {
        // add typeName and isDeleted predicates
        // the lambda parameter i is an item of TInterface type
        var queryable = Enumerable.Empty<TItem>().AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);

        return new SqlQueryCommand(
            expressionConverter: expressionConverter,
            queryable: queryable,
            dataOptions: dataOptions,
            convertToQueryResult: convertToQueryResult);
    }

    /// <summary>
    /// Save the item to the backing store.
    /// </summary>
    /// <param name="dataConnection">The data connection.</param>
    /// <param name="request">The save request with item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The result of the save operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="SaveAction"/> is not recognized.</exception>
    private static async Task<TItem> SaveItemAsync(
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

    private class SqlQueryCommand(
        ExpressionConverter<TInterface, TItem> expressionConverter,
        IQueryable<TItem> queryable,
        DataOptions dataOptions,
        Func<TItem, IQueryResult<TInterface>> convertToQueryResult)
        : QueryCommand<TInterface, TItem>(expressionConverter, queryable)
    {
        /// <summary>
        /// Execute the underlying <see cref="IQueryable{TItem}"/> and return the results as an async enumerable.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="IAsyncEnumerable{TInterface}"/>.</returns>
        protected override async IAsyncEnumerable<IQueryResult<TInterface>> ExecuteAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // create the connection
            using var dataConnection = new DataConnection(dataOptions);

            // create the query from the table and the queryable expression
            var queryable = dataConnection
                .GetTable<TItem>()
                .Provider
                .CreateQuery<TItem>(GetQueryable().Expression);

            foreach (var item in queryable.AsEnumerable())
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return await Task.FromResult(convertToQueryResult(item));
            }
        }
    }

    [GeneratedRegex(@"^Violation of PRIMARY KEY constraint ")]
    private static partial Regex PrimaryKeyViolationRegex();


    [GeneratedRegex(@"^Precondition Failed\.$")]
    private static partial Regex PreconditionFailedRegex();
}
