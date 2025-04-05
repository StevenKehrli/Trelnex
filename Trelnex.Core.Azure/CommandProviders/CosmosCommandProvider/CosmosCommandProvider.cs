using System.Net;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// An implementation of <see cref="ICommandProvider{TInterface}"/> that uses a CosmosDB container as a backing store.
/// </summary>
internal class CosmosCommandProvider<TInterface, TItem>(
    Container container,
    string typeName,
    IValidator<TItem>? validator = null,
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
            return await container.ReadItemAsync<TItem>(
                id: id,
                partitionKey: new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);
        }
        catch (CosmosException ce) when (ce.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        catch (CosmosException ex)
        {
            throw new CommandException(ex.StatusCode, ex.Message);
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

        // create the batch operation
        var batch = container.CreateTransactionalBatch(
            new PartitionKey(partitionKey));

        // add the items to the batch
        for (var index = 0; index < requests.Length; index++)
        {
            AddItem(batch, requests[index]);
        }

        try
        {
            // execute the batch
            using var response = await batch.ExecuteAsync(cancellationToken);

            for (var index = 0; index < requests.Length; index++)
            {
                // get the returned item
                // the operation results are interleaved between the item and event, so:
                //   request 0 item is at index 0 and its event it at index 1
                //   request 1 item is at index 2 and its event it at index 3
                //   etc
                var itemResponse = response.GetOperationResultAtIndex<TItem>(index * 2);

                // check the status code and build the result
                var httpStatusCode = itemResponse.IsSuccessStatusCode
                    ? HttpStatusCode.OK
                    : itemResponse.StatusCode;

                var item = itemResponse.IsSuccessStatusCode
                    ? itemResponse.Resource
                    : null;

                saveResults[index] = new SaveResult<TInterface, TItem>(
                    httpStatusCode,
                    item);
            }

            return saveResults;
        }
        catch (CosmosException ce)
        {
            throw new CommandException(ce.StatusCode, ce.Message);
        }
    }

    /// <summary>
    /// Create the <see cref="IQueryable{TItem}"/> to query the items.
    /// </summary>
    /// <returns></returns>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // add typeName and isDeleted predicates
        // the lambda parameter i is an item of TInterface type
        return container
            .GetItemLinqQueryable<TItem>()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted.IsDefined() == false || i.IsDeleted == false);
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
        // get the feed iterator
        var feedIterator = queryable.ToFeedIterator();

        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TItem>? feedResponse = null;

            try
            {
                // this is where cosmos will throw
                feedResponse = feedIterator.ReadNextAsync(cancellationToken).Result;
            }
            catch (CosmosException ex)
            {
                throw new CommandException(ex.StatusCode);
            }

            foreach (var item in feedResponse)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Add the item to the batch.
    /// </summary>
    /// <param name="batch">The <see cref="TransactionalBatch"/> to add the item to.</param>
    /// <param name="request">The save request with item and event to save.</param>
    /// <returns>The <see cref="TransactionalBatch"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="SaveAction"/> is not recognized.</exception>
    private static TransactionalBatch AddItem(
        TransactionalBatch batch,
        SaveRequest<TInterface, TItem> request) => request.SaveAction switch
    {
        SaveAction.CREATED => batch
            .CreateItem(
                item: request.Item)
            .CreateItem(
                item: request.Event),

        SaveAction.UPDATED or SaveAction.DELETED => batch
            .ReplaceItem(
                id: request.Item.Id,
                item: request.Item,
                requestOptions: new TransactionalBatchItemRequestOptions { IfMatchEtag = request.Item.ETag })
            .CreateItem(
                item: request.Event),

        _ => throw new InvalidOperationException($"Unrecognized SaveAction: {request.SaveAction}")
    };
}
