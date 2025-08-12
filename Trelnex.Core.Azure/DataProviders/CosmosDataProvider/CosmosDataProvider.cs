using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Azure.DataProviders;

/// <summary>
/// Cosmos DB implementation of <see cref="DataProvider{TInterface, TItem}"/>.
/// </summary>
/// <typeparam name="TInterface">The interface that the item implements.</typeparam>
/// <typeparam name="TItem">The type of the item to store in Cosmos DB.</typeparam>
/// <param name="container">The Cosmos DB container to interact with.  Must not be null.</param>
/// <param name="typeName">The type name used to filter items.  Must not be null or empty.</param>
/// <param name="validator">Optional validator for items before they are saved.  Can be null.</param>
/// <param name="commandOperations">Optional command operations to override default behaviors. Can be null.</param>
/// <param name="eventTimeToLive">Optional time-to-live for events in the container.</param>
/// <param name="blockCipherService">Optional block cipher service for encrypting sensitive data.</param>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> or <paramref name="typeName"/> is null.</exception>
internal class CosmosDataProvider<TInterface, TItem>(
    Container container,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null,
    int? eventTimeToLive = null,
    IBlockCipherService? blockCipherService = null)
    : DataProvider<TInterface, TItem>(typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Fields

    /// <summary>
    /// JSON serializer options for Cosmos DB.
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = blockCipherService is not null
            ? new EncryptedJsonTypeInfoResolver(blockCipherService)
            : null
    };

    #endregion

    #region Protected Methods

    /// <summary>
    /// Creates a queryable for Cosmos DB that filters by type name and deleted status.
    /// </summary>
    /// <returns>An <see cref="IQueryable{TItem}"/> for the container.</returns>
    /// <remarks>Adds standard predicate filters to exclude deleted items and filter by type name.</remarks>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Add typeName and isDeleted predicates to filter items
        return container
            .GetItemLinqQueryable<TItem>()
            .Where(item => item.TypeName == TypeName)
            .Where(item => item.IsDeleted.IsDefined() == false || item.IsDeleted == false);
    }

    /// <summary>
    /// Executes a query against Cosmos DB and returns the results.
    /// </summary>
    /// <param name="queryable">The queryable to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An enumerable of items matching the query.</returns>
    /// <exception cref="CommandException">When a Cosmos DB exception occurs during query execution.</exception>
    /// <remarks>Uses the Cosmos DB feed iterator to page through results.</remarks>
#pragma warning disable CS8425
    [TraceMethod]
    protected override async IAsyncEnumerable<TItem> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // Convert the LINQ queryable to a SQL query definition for Cosmos DB
        var queryDefinition = queryable.ToQueryDefinition();

        // Get the stream-based feed iterator
        using var feedIterator = container.GetItemQueryStreamIterator(queryDefinition);

        // Iterate through results using the feed iterator
        while (feedIterator.HasMoreResults)
        {
            ResponseMessage? responseMessage = null;

            try
            {
                // Execute the query and get the next page of stream results
                // This is where cosmos will throw exceptions if there are issues with the query or connection
                responseMessage = await feedIterator.ReadNextAsync(cancellationToken);

                // Check if the response is successful.  If not, throw an exception.
                if (responseMessage.IsSuccessStatusCode is false)
                {
                    throw new CommandException(responseMessage.StatusCode, responseMessage.ErrorMessage);
                }
            }
            catch (CosmosException exception)
            {
                throw new CommandException(exception.StatusCode);
            }

            // Parse the JSON response from the stream
            using var jsonDocument = JsonDocument.Parse(responseMessage.Content);

            // Enumerate the elements in the response and deserialize them into TItem objects
            foreach (var jsonElement in jsonDocument.RootElement.GetProperty("Documents").EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = jsonElement.Deserialize<TItem>(_jsonSerializerOptions);

                if (item is null) continue;

                yield return item;
            }
        }
    }
#pragma warning restore CS8425

    /// <summary>
    /// Reads an item from the Cosmos DB container.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The item if found, or <see langword="null"/> if the item does not exist.</returns>
    /// <exception cref="CommandException">When a Cosmos DB exception occurs during the read operation.</exception>
    [TraceMethod]
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Attempt to read the item as a stream from Cosmos DB.
            using var responseMessage = await container.ReadItemStreamAsync(
                id: id,
                partitionKey: new PartitionKey(partitionKey),
                cancellationToken: cancellationToken);

            // Check if the response is successful.
            if (responseMessage.IsSuccessStatusCode is false)
            {
                // If the item is not found, return null. Otherwise, throw an exception.
                return responseMessage.StatusCode == HttpStatusCode.NotFound
                    ? null
                    : throw new CommandException(responseMessage.StatusCode, responseMessage.ErrorMessage);
            }

            // Deserialize the item from the response stream.
            using var sr = new StreamReader(responseMessage.Content);
            var item = JsonSerializer.Deserialize<TItem>(
                utf8Json: sr.BaseStream,
                options: _jsonSerializerOptions);

            // Ensure the item is of the expected type.
            return item?.TypeName == TypeName
                ? item
                : null;
        }
        catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.NotFound)
        {
            // Return default if the item is not found.
            return default;
        }
        catch (CosmosException cosmosException)
        {
            throw new CommandException(cosmosException.StatusCode, cosmosException.Message);
        }
    }

    /// <summary>
    /// Saves a batch of items in Cosmos DB as an atomic transaction.
    /// </summary>
    /// <param name="requests">Array of save requests to process.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Array of save results with status codes and saved items.</returns>
    /// <exception cref="CommandException">When a Cosmos DB exception occurs during the batch operation.</exception>
    /// <remarks>Uses the Cosmos DB transactional batch API to ensure atomicity.</remarks>
    [TraceMethod]
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Initialize an array to hold the results of each save operation in the batch.
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // Extract the partition key from the first request.  All items in a batch must have the same partition key.
        var partitionKey = requests.First().Item.PartitionKey;

        // Create a transactional batch for the specified partition key.
        var batch = container.CreateTransactionalBatch(
            new PartitionKey(partitionKey));

        // Add each item and its corresponding event to the batch.
        var requestStreams = new SaveRequestStream[requests.Length];
        for (var index = 0; index < requests.Length; index++)
        {
            // Create a stream for the item and event to be saved
            requestStreams[index] = ConvertSaveRequestToStream(
                saveRequest: requests[index]);

            // Add the item and event to the transactional batch
            AddItem(
                batch: batch,
                saveRequestStream: requestStreams[index]);
        }

        try
        {
            // Execute the batch as an atomic transaction
            using var response = await batch.ExecuteAsync(cancellationToken);

            // Process the results of each operation in the batch
            for (var index = 0; index < requests.Length; index++)
            {
                // Get the returned item
                // The operation results are interleaved between the item and event, so:
                //   request 0 item is at index 0 and its event is at index 1
                //   request 1 item is at index 2 and its event is at index 3
                //   etc
                var itemResult = response[index * 2];

                // Parse the item response to get the HTTP status code and item
                saveResults[index] = ParseSaveResult(itemResult);
            }

            // Return the array of save results
            return saveResults;
        }
        catch (CosmosException cosmosException)
        {
            throw new CommandException(cosmosException.StatusCode, cosmosException.Message);
        }
        finally
        {
            // Ensure all streams are disposed of to prevent memory leaks
            Array.ForEach(requestStreams, requestStream =>
            {
                requestStream.Dispose();
            });
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Adds an item and its event to a transactional batch.
    /// </summary>
    /// <param name="batch">The batch to add operations to.</param>
    /// <param name="saveRequestStream">The save request stream containing the item and event.</param>
    /// <returns>The transactional batch with operations added.</returns>
    /// <exception cref="InvalidOperationException">When the <paramref name="saveRequestStream"/> has an unrecognized <see cref="SaveAction"/>.</exception>
    /// <remarks>Handles different operations based on the save action.</remarks>
    private static TransactionalBatch AddItem(
        TransactionalBatch batch,
        SaveRequestStream saveRequestStream) => saveRequestStream.SaveAction switch
        {
            // If the item is being created, create both the item and the event
            SaveAction.CREATED => batch
                .CreateItemStream(
                    streamPayload: saveRequestStream.ItemStream)
                .CreateItemStream(
                    streamPayload: saveRequestStream.EventStream),

            // If the item is being updated or deleted, replace the item and create the event
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

            // If the SaveAction is not recognized, throw an exception
            _ => throw new InvalidOperationException($"Unknown SaveAction: {saveRequestStream.SaveAction}")
        };

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts a SaveRequest to a SaveRequestStream.
    /// </summary>
    /// <param name="saveRequest">The save request to convert.</param>
    /// <returns>A SaveRequestStream containing the serialized item and event streams.</returns>
    private SaveRequestStream ConvertSaveRequestToStream(
        SaveRequest<TInterface, TItem> saveRequest)
    {
        // Serialize the item to a stream
        var itemStream = new MemoryStream();
        JsonSerializer.Serialize(
            utf8Json: itemStream,
            value: saveRequest.Item,
            options: _jsonSerializerOptions);
        itemStream.Position = 0;

        // Create a new event with expiration ("ttl")
        // Serialize the event to a stream
        var eventWithExpiration = new ItemEventWithExpiration(saveRequest.Event, eventTimeToLive);
        var eventStream = new MemoryStream();
        JsonSerializer.Serialize(
            utf8Json: eventStream,
            value: eventWithExpiration,
            options: _jsonSerializerOptions);
        eventStream.Position = 0;

        // Create and return a new SaveRequestStream instance
        return new SaveRequestStream(
            Id: saveRequest.Item.Id,
            ETag: saveRequest.Item.ETag,
            SaveAction: saveRequest.SaveAction,
            ItemStream: itemStream,
            EventStream: eventStream);
    }

    /// <summary>
    /// Parses the result of a transactional batch operation and returns a SaveResult.
    /// </summary>
    /// <param name="itemResult">The result of the transactional batch operation.</param>
    /// <returns>A SaveResult containing the status code and the deserialized item, or null if the operation was not successful.</returns>
    private SaveResult<TInterface, TItem> ParseSaveResult(
        TransactionalBatchOperationResult itemResult)
    {
        // If the operation was not successful, return the status code and null item
        if (itemResult.IsSuccessStatusCode is false)
        {
            return new SaveResult<TInterface, TItem>(
                itemResult.StatusCode,
                null);
        }

        // Deserialize the item from the response stream
        using var sr = new StreamReader(itemResult.ResourceStream);
        var item = JsonSerializer.Deserialize<TItem>(
            utf8Json: sr.BaseStream,
            options: _jsonSerializerOptions);

        // Return the OK status code and the deserialized item
        return new SaveResult<TInterface, TItem>(
            HttpStatusCode.OK,
            item);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// A helper class to manage the streams for the item and event being saved.
    /// Implements the <see cref="IDisposable"/> interface to ensure resources are released.
    /// </summary>
    /// <param name="Id">The ID of the item.</param>
    /// <param name="ETag">The ETag of the item.</param>
    /// <param name="SaveAction">The save action being performed.</param>
    /// <param name="ItemStream">The stream containing the item data.</param>
    /// <param name="EventStream">The stream containing the event data.</param>
    private record SaveRequestStream(
        string Id,
        string? ETag,
        SaveAction SaveAction,
        Stream ItemStream,
        Stream EventStream) : IDisposable
    {
        #region IDisposable

        /// <summary>
        /// Disposes of the resources held by this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources held by this instance.
        /// </summary>
        /// <param name="disposing">True if called from the <see cref="Dispose()"/> method; otherwise, false.</param>
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
