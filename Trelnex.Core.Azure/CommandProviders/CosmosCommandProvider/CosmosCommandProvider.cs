using System.Net;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Trelnex.Core.Data;

namespace Trelnex.Core.Azure.CommandProviders;

/// <summary>
/// SQL Server implementation of <see cref="DbCommandProvider{TInterface, TItem}"/>.
/// </summary>
/// <typeparam name="TInterface">Interface type for the items.</typeparam>
/// <typeparam name="TItem">Concrete implementation type for the items.</typeparam>
/// <remarks>Provides Cosmos DB-specific implementations of the abstract methods.</remarks>
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
    /// Reads an item from the Cosmos DB container.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The item if found, or <see langword="null"/> if the item does not exist.</returns>
    /// <exception cref="CommandException">When a Cosmos DB exception occurs during the read operation.</exception>
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to read the item from Cosmos DB.
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
    /// Saves a batch of items in Cosmos DB as an atomic transaction.
    /// </summary>
    /// <param name="requests">Array of save requests to process.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Array of save results with status codes and saved items.</returns>
    /// <exception cref="CommandException">When a Cosmos DB exception occurs during the batch operation.</exception>
    /// <remarks>Uses the Cosmos DB transactional batch API.</remarks>
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Initialize an array to hold the results of each save operation in the batch.
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // Extract the partition key from the first request.
        var partitionKey = requests.First().Item.PartitionKey;

        // Create a transactional batch for the specified partition key.
        var batch = container.CreateTransactionalBatch(
            new PartitionKey(partitionKey));

        // Add each item and its corresponding event to the batch.
        for (var index = 0; index < requests.Length; index++)
        {
            AddItem(batch, requests[index]);
        }

        try
        {
            // Execute the batch
            using var response = await batch.ExecuteAsync(cancellationToken);

            // Process the results
            for (var index = 0; index < requests.Length; index++)
            {
                // Get the returned item
                // The operation results are interleaved between the item and event, so:
                //   request 0 item is at index 0 and its event is at index 1
                //   request 1 item is at index 2 and its event is at index 3
                //   etc
                var itemResponse = response.GetOperationResultAtIndex<TItem>(index * 2);

                // Check the status code and build the result
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
    /// Creates a queryable for Cosmos DB that filters by type name and deleted status.
    /// </summary>
    /// <returns>An <see cref="IQueryable{TItem}"/> for the container.</returns>
    /// <remarks>Adds standard predicate filters.</remarks>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Add typeName and isDeleted predicates
        return container
            .GetItemLinqQueryable<TItem>()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted.IsDefined() == false || i.IsDeleted == false);
    }

    /// <summary>
    /// Executes a query against Cosmos DB and returns the results.
    /// </summary>
    /// <param name="queryable">The queryable to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An enumerable of items matching the query.</returns>
    /// <exception cref="CommandException">When a Cosmos DB exception occurs during query execution.</exception>
    /// <remarks>Uses the Cosmos DB feed iterator to page through results.</remarks>
    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Get the feed iterator
        var feedIterator = queryable.ToFeedIterator();

        // Iterate through results
        while (feedIterator.HasMoreResults)
        {
            FeedResponse<TItem>? feedResponse = null;

            try
            {
                // This is where cosmos will throw
                feedResponse = feedIterator.ReadNextAsync(cancellationToken).GetAwaiter().GetResult();
            }
            catch (CosmosException ex)
            {
                throw new CommandException(ex.StatusCode);
            }

            // Yield each item
            foreach (var item in feedResponse)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Adds an item and its event to a transactional batch.
    /// </summary>
    /// <param name="batch">The batch to add operations to.</param>
    /// <param name="request">The save request containing the item and event.</param>
    /// <returns>The transactional batch with operations added.</returns>
    /// <exception cref="InvalidOperationException">When the <paramref name="request"/> has an unrecognized <see cref="SaveAction"/>.</exception>
    /// <remarks>Handles different operations based on the save action.</remarks>
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
