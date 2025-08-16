using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Cosmos DB implementation of data provider for storing and retrieving items.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
/// <param name="container">Cosmos DB container for data operations.</param>
/// <param name="typeName">Type name identifier for filtering items.</param>
/// <param name="itemValidator">Optional validator for items before saving.</param>
/// <param name="commandOperations">Optional CRUD operations override.</param>
/// <param name="eventTimeToLive">Optional TTL for events in seconds.</param>
/// <param name="blockCipherService">Optional encryption service for sensitive data.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
/// <exception cref="ArgumentNullException">Thrown when container or typeName is null.</exception>
internal class CosmosDataProvider<TItem>(
    string typeName,
    Container container,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null,
    int? eventTimeToLive = null,
    IBlockCipherService? blockCipherService = null,
    ILogger? logger = null)
    : DataProvider<TItem>(
        typeName: typeName,
        itemValidator: itemValidator,
        commandOperations: commandOperations,
        blockCipherService: blockCipherService,
        logger: logger)
    where TItem : BaseItem, new()
{
    #region Protected Methods

    /// <summary>
    /// Creates a LINQ queryable for Cosmos DB with standard filters applied.
    /// </summary>
    /// <returns>Queryable filtered by type name and deletion status.</returns>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Apply type name and deletion status filters to container queryable
        return container
            .GetItemLinqQueryable<TItem>()
            .Where(item => item.TypeName == TypeName)
            .Where(item => item.IsDeleted.IsDefined() == false || item.IsDeleted == false);
    }

    /// <summary>
    /// Executes a LINQ query against Cosmos DB and streams results.
    /// </summary>
    /// <param name="queryable">LINQ queryable to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous enumerable of matching items.</returns>
    /// <exception cref="CommandException">Thrown when Cosmos DB operations fail.</exception>
#pragma warning disable CS8425
    [TraceMethod]
    protected override async IAsyncEnumerable<TItem> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Convert LINQ query to Cosmos DB SQL query
        var queryDefinition = queryable.ToQueryDefinition();

        // Execute query using stream-based feed iterator
        using var feedIterator = container.GetItemQueryStreamIterator(queryDefinition);

        while (feedIterator.HasMoreResults)
        {
            ResponseMessage? responseMessage = null;

            try
            {
                // Get next page of results from Cosmos DB
                responseMessage = await feedIterator.ReadNextAsync(cancellationToken);

                // Verify response was successful
                if (responseMessage.IsSuccessStatusCode is false)
                {
                    throw new CommandException(responseMessage.StatusCode, responseMessage.ErrorMessage);
                }
            }
            catch (CosmosException exception)
            {
                throw new CommandException(exception.StatusCode);
            }

            // Parse JSON response and deserialize items
            using var jsonDocument = JsonDocument.Parse(responseMessage.Content);

            foreach (var jsonElement in jsonDocument.RootElement.GetProperty("Documents").EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = DeserializeItem(jsonElement.GetRawText());

                if (item is null) continue;

                yield return item;
            }
        }
    }
#pragma warning restore CS8425

    /// <summary>
    /// Retrieves a single item from Cosmos DB using its identifiers.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="partitionKey">Partition key for the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Item if found and matches type, null otherwise.</returns>
    /// <exception cref="CommandException">Thrown when Cosmos DB operations fail.</exception>
    [TraceMethod]
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Read item from Cosmos DB as stream
            using var responseMessage = await container.ReadItemStreamAsync(
                id: id,
                partitionKey: new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            // Handle response based on status code
            if (responseMessage.IsSuccessStatusCode is false)
            {
                return responseMessage.StatusCode == HttpStatusCode.NotFound
                    ? null
                    : throw new CommandException(responseMessage.StatusCode, responseMessage.ErrorMessage);
            }

            // Deserialize item from response stream
            using var sr = new StreamReader(responseMessage.Content);
            var item = DeserializeItem(sr.BaseStream);

            // Verify item matches expected type
            return item?.TypeName == TypeName
                ? item
                : null;
        }
        catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
        catch (CosmosException cosmosException)
        {
            throw new CommandException(cosmosException.StatusCode, cosmosException.Message);
        }
    }

    /// <summary>
    /// Saves multiple items and events atomically using Cosmos DB transactional batch.
    /// </summary>
    /// <param name="requests">Array of save requests to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of save results for each request.</returns>
    /// <exception cref="CommandException">Thrown when Cosmos DB operations fail.</exception>
    [TraceMethod]
    protected override async Task<SaveResult<TItem>[]> SaveBatchAsync(
        SaveRequest<TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Initialize results array for all requests
        var saveResults = new SaveResult<TItem>[requests.Length];

        // All items in a batch must share the same partition key
        var partitionKey = requests.First().Item.PartitionKey;

        // Create Cosmos DB transactional batch
        var batch = container.CreateTransactionalBatch(
            new PartitionKey(partitionKey));

        // Convert requests to streams and add to batch
        var requestStreams = new SaveRequestStream[requests.Length];
        for (var index = 0; index < requests.Length; index++)
        {
            requestStreams[index] = ConvertSaveRequestToStream(
                saveRequest: requests[index]);

            AddItem(
                batch: batch,
                saveRequestStream: requestStreams[index]);
        }

        try
        {
            // Execute batch atomically
            using var response = await batch.ExecuteAsync(cancellationToken);

            // Process results for each operation
            for (var index = 0; index < requests.Length; index++)
            {
                // Results are interleaved: item at even indices, events at odd indices
                var itemResult = response[index * 2];

                saveResults[index] = ParseSaveResult(itemResult);
            }

            return saveResults;
        }
        catch (CosmosException cosmosException)
        {
            throw new CommandException(cosmosException.StatusCode, cosmosException.Message);
        }
        finally
        {
            // Clean up request streams to prevent memory leaks
            Array.ForEach(requestStreams, requestStream =>
            {
                requestStream.Dispose();
            });
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Adds item and event operations to a transactional batch based on save action.
    /// </summary>
    /// <param name="batch">Transactional batch to add operations to.</param>
    /// <param name="saveRequestStream">Stream containing item and event data.</param>
    /// <returns>Batch with operations added.</returns>
    /// <exception cref="InvalidOperationException">Thrown for unrecognized save actions.</exception>
    private static TransactionalBatch AddItem(
        TransactionalBatch batch,
        SaveRequestStream saveRequestStream) => saveRequestStream.SaveAction switch
        {
            // Create operations for new items
            SaveAction.CREATED => batch
                .CreateItemStream(
                    streamPayload: saveRequestStream.ItemStream)
                .CreateItemStream(
                    streamPayload: saveRequestStream.EventStream),

            // Replace operations for updates and deletes with ETag check
            SaveAction.UPDATED or SaveAction.DELETED => batch
                .ReplaceItemStream(
                    id: saveRequestStream.Id,
                    streamPayload: saveRequestStream.ItemStream,
                    requestOptions: new TransactionalBatchItemRequestOptions
                    {
                        IfMatchEtag = saveRequestStream.ETag
                    })
                .CreateItemStream(
                    streamPayload: saveRequestStream.EventStream),

            _ => throw new InvalidOperationException($"Unknown SaveAction: {saveRequestStream.SaveAction}")
        };

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts a save request to stream format for Cosmos DB batch operations.
    /// </summary>
    /// <param name="saveRequest">Save request to convert.</param>
    /// <returns>Stream representation with serialized item and event data.</returns>
    private SaveRequestStream ConvertSaveRequestToStream(
        SaveRequest<TItem> saveRequest)
    {
        // Serialize item to memory stream
        var itemStream = new MemoryStream();
        SerializeItem(
            utf8Json: itemStream,
            item: saveRequest.Item);

        // Create event with TTL and serialize to memory stream
        var eventWithExpiration = new ItemEventWithExpiration(saveRequest.Event, eventTimeToLive);
        var eventStream = new MemoryStream();
        SerializeEvent(
            utf8Json: eventStream,
            itemEvent: eventWithExpiration);
        eventStream.Position = 0;

        return new SaveRequestStream(
            Id: saveRequest.Item.Id,
            ETag: saveRequest.Item.ETag,
            SaveAction: saveRequest.SaveAction,
            ItemStream: itemStream,
            EventStream: eventStream);
    }

    /// <summary>
    /// Parses batch operation result and creates save result with status and item.
    /// </summary>
    /// <param name="itemResult">Batch operation result to parse.</param>
    /// <returns>Save result with status code and deserialized item or null.</returns>
    private SaveResult<TItem> ParseSaveResult(
        TransactionalBatchOperationResult itemResult)
    {
        // Return failure status if operation was not successful
        if (itemResult.IsSuccessStatusCode is false)
        {
            return new SaveResult<TItem>(
                itemResult.StatusCode,
                null);
        }

        // Deserialize successful result item
        using var sr = new StreamReader(itemResult.ResourceStream);
        var item = DeserializeItem(sr.BaseStream);

        return new SaveResult<TItem>(
            HttpStatusCode.OK,
            item);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Disposable container for serialized item and event streams used in batch operations.
    /// </summary>
    /// <param name="Id">Item identifier.</param>
    /// <param name="ETag">Item ETag for optimistic concurrency.</param>
    /// <param name="SaveAction">Type of save operation being performed.</param>
    /// <param name="ItemStream">Stream containing serialized item data.</param>
    /// <param name="EventStream">Stream containing serialized event data.</param>
    private record SaveRequestStream(
        string Id,
        string? ETag,
        SaveAction SaveAction,
        Stream ItemStream,
        Stream EventStream) : IDisposable
    {
        #region IDisposable

        /// <summary>
        /// Disposes of managed resources held by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases managed resources when disposing.
        /// </summary>
        /// <param name="disposing">True when called from Dispose method.</param>
        protected virtual void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                ItemStream?.Dispose();
                EventStream?.Dispose();
            }
        }

        #endregion
    }

    #endregion
}
