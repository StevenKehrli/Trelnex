using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// An in-memory implementation of <see cref="CommandProvider{TInterface, TItem}"/> that stores data
/// in memory for testing and prototyping scenarios.
/// </summary>
/// <typeparam name="TInterface">The interface type that defines the data contract.</typeparam>
/// <typeparam name="TItem">The concrete class that implements <typeparamref name="TInterface"/> and inherits from <see cref="BaseItem"/>.</typeparam>
/// <remarks>
/// <para>
/// This is a temporary store in memory for item storage and retrieval, primarily designed for testing
/// and development scenarios where persistence is not required or desirable.
/// </para>
/// <para>
/// This command provider will serialize the item to a string for storage and deserialize the string
/// to an item for retrieval. This validates that the item is JSON attributed correctly for a
/// persistent backing store like Cosmos DB, while maintaining all data in memory.
/// </para>
/// <para>
/// Thread safety is ensured through the use of a <see cref="ReaderWriterLockSlim"/> to allow
/// concurrent reads but exclusive writes.
/// </para>
/// </remarks>
internal class InMemoryCommandProvider<TInterface, TItem>(
    string typeName,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null)
    : CommandProvider<TInterface, TItem>(typeName, itemValidator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Fields

    /// <summary>
    /// An exclusive lock to ensure that only one operation that modifies the backing store is in progress at a time.
    /// </summary>
    /// <remarks>
    /// This lock uses a reader-writer pattern, allowing multiple simultaneous reads but exclusive writes.
    /// </remarks>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// The in-memory backing store containing all items and their associated events.
    /// </summary>
    private InMemoryStore _store = new();

    #endregion

    #region Protected Methods

    /// <summary>
    /// Creates an <see cref="IQueryable{T}"/> for querying items in the in-memory store.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable{TItem}"/> that filters items by type name and deleted status.
    /// </returns>
    /// <remarks>
    /// This method uses deferred execution, so the actual query against the store
    /// happens when the queryable is enumerated in <see cref="ExecuteQueryable"/>.
    /// </remarks>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Deferred execution, so we don't need to lock here.
        // The base filter ensures we only return items matching our type name
        // and excludes soft-deleted items.
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Executes the specified LINQ query against the in-memory store and returns the results.
    /// </summary>
    /// <param name="queryable">The LINQ query to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An enumerable of items matching the query criteria.
    /// </returns>
    /// <remarks>
    /// This method acquires a read lock to ensure thread safety while the query is executing.
    /// It transforms the original queryable (which was based on an empty collection) to operate
    /// against the actual in-memory store data.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Acquire a read lock to ensure thread safety
            _lock.EnterReadLock();

            // Extract the method call expression that contains the filter predicates
            var mce = (queryable.Expression as MethodCallExpression)!;

            // Replace the empty source with our actual data store
            var constantValue = _store.AsQueryable();
            var constantExpression = Expression.Constant(constantValue);

            // Create a new method call expression with our store as the data source
            var methodCallExpression = Expression.Call(
                mce.Method,
                constantExpression,
                mce.Arguments[1]!);

            // Create the query from the store and the method call expression
            var queryableFromExpression = _store
                .AsQueryable()
                .Provider
                .CreateQuery<TItem>(methodCallExpression);

            // Yield each matching item
            foreach (var item in queryableFromExpression.AsEnumerable())
            {
                // Check for cancellation before yielding each item
                cancellationToken.ThrowIfCancellationRequested();

                yield return item;
            }
        }
        finally
        {
            // Always release the lock, even if an exception occurs
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Reads an item from the in-memory store by ID and partition key.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the item if found,
    /// or <see langword="null"/> if the item does not exist.
    /// </returns>
    /// <remarks>
    /// This method acquires a read lock to ensure thread safety during the read operation.
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Acquire a read lock to ensure thread safety
            _lock.EnterReadLock();

            // Read from the backing store
            var read = _store.ReadItem(id, partitionKey);

            // Return the result
            return await Task.FromResult<TItem?>(read);
        }
        finally
        {
            // Always release the lock, even if an exception occurs
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Saves a batch of items in the in-memory store as an atomic transaction.
    /// </summary>
    /// <param name="partitionKey">The partition key common to all items in the batch.</param>
    /// <param name="requests">An array of save requests, each containing an item and its associated event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an array of
    /// <see cref="SaveResult{TInterface, TItem}"/> objects, one for each request in the batch.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method implements atomic batch processing by:
    /// 1. Creating a copy of the current store
    /// 2. Applying all operations to the copy
    /// 3. If all operations succeed, replacing the original store with the copy
    /// 4. If any operation fails, discarding the copy and marking all operations as failed
    /// </para>
    /// <para>
    /// This ensures that either all operations in the batch succeed or none of them take effect.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        string partitionKey,
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Allocate the results array
        var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

        // Acquire an exclusive write lock
        _lock.EnterWriteLock();

        // Create a copy of the existing backing store to use for the batch
        // This allows us to roll back if any operation fails
        var batchStore = new InMemoryStore(_store);

        // Process each item in the batch
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            var saveRequest = requests[saveRequestIndex];

            try
            {
                // Attempt to save the item to the batch store
                var saved = SaveItem(batchStore, saveRequest);

                // Record successful result
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (ex is CommandException || ex is InvalidOperationException)
            {
                // Determine the appropriate status code for the failure
                var httpStatusCode = ex is CommandException commandEx
                    ? commandEx.HttpStatusCode
                    : HttpStatusCode.InternalServerError;

                // Record the failure
                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        httpStatusCode,
                        null);

                // Exit the loop on first failure
                break;
            }
        }

        // Check if the entire batch was processed successfully
        if (saveRequestIndex == requests.Length)
        {
            // The batch completed successfully, update the backing store
            _store = batchStore;
        }
        else
        {
            // A save request failed - mark all other requests as dependent failures
            // This ensures clients understand the batch transaction semantics
            for (var saveResultIndex = 0; saveResultIndex < saveResults.Length; saveResultIndex++)
            {
                // Skip the request that actually failed (it keeps its original error code)
                if (saveResultIndex == saveRequestIndex) continue;

                saveResults[saveResultIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.FailedDependency,
                        null);
            }
        }

        // Always release the write lock
        _lock.ExitWriteLock();

        return await Task.FromResult(saveResults);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Clears all data from the in-memory store.
    /// </summary>
    /// <remarks>
    /// This method is primarily used for testing purposes to reset the store to an empty state.
    /// </remarks>
    internal void Clear()
    {
        try
        {
            _lock.EnterWriteLock();

            _store = new();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets all events stored in the in-memory store.
    /// </summary>
    /// <returns>
    /// An array of all <see cref="ItemEvent{TItem}"/> objects in the store.
    /// </returns>
    /// <remarks>
    /// This method is primarily used for testing and debugging purposes.
    /// </remarks>
    internal ItemEvent<TItem>[] GetEvents()
    {
        try
        {
            _lock.EnterReadLock();

            return _store.GetEvents();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Generates a composite key for storing and retrieving items.
    /// </summary>
    /// <param name="item">The item to generate a key for.</param>
    /// <returns>
    /// A string key in the format "partitionKey:id".
    /// </returns>
    private static string GetItemKey(
        BaseItem item)
    {
        return GetItemKey(
            partitionKey: item.PartitionKey,
            id: item.Id);
    }

    /// <summary>
    /// Generates a composite key for storing and retrieving items.
    /// </summary>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="id">The id of the item.</param>
    /// <returns>
    /// A string key in the format "partitionKey:id".
    /// </returns>
    private static string GetItemKey(
        string partitionKey,
        string id)
    {
        return $"{partitionKey}:{id}";
    }

    /// <summary>
    /// Saves an item to the specified backing store.
    /// </summary>
    /// <param name="store">The backing store to save the item to.</param>
    /// <param name="request">The save request with item and event to save.</param>
    /// <returns>
    /// The saved item after processing.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="SaveAction"/> is not recognized.
    /// </exception>
    /// <exception cref="CommandException">
    /// Thrown when a storage operation fails, with a relevant HTTP status code.
    /// </exception>
    private static TItem SaveItem(
        InMemoryStore store,
        SaveRequest<TInterface, TItem> request) => request.SaveAction switch
    {
        SaveAction.CREATED =>
            store.CreateItem(request.Item, request.Event),

        SaveAction.UPDATED or SaveAction.DELETED =>
            store.UpdateItem(request.Item, request.Event),

        _ => throw new InvalidOperationException($"Unrecognized SaveAction: {request.SaveAction}")
    };

    #endregion

    #region Nested Types

    /// <summary>
    /// Base class for serialized items and events stored in memory.
    /// </summary>
    /// <typeparam name="T">The type of item being serialized, must derive from <see cref="BaseItem"/>.</typeparam>
    /// <remarks>
    /// This class handles the serialization and deserialization of items and events,
    /// ensuring they are stored in a format similar to how they would be in a real database.
    /// </remarks>
    private abstract class BaseSerialized<T> where T : BaseItem
    {
        #region Private Static Fields

        /// <summary>
        /// The JSON serializer options used for serialization and deserialization.
        /// </summary>
        private static readonly JsonSerializerOptions _options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        #endregion

        #region Private Fields

        /// <summary>
        /// The serialized JSON string representation of the item.
        /// </summary>
        private string _jsonString = null!;

        /// <summary>
        /// The ETag value for optimistic concurrency control.
        /// </summary>
        private string _eTag = null!;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the deserialized resource from the stored JSON.
        /// </summary>
        /// <remarks>
        /// This property deserializes the JSON string each time it's accessed,
        /// ensuring any modifications to the JSON are reflected in the returned object.
        /// </remarks>
        public T Resource
        {
            get
            {
                // Deserialize the JSON string to the resource type
                var resource = JsonSerializer.Deserialize<T>(_jsonString, _options)!;

                // Set the ETag from the storage metadata
                resource.ETag = _eTag;

                // Return the fully reconstructed resource
                return resource;
            }
        }

        /// <summary>
        /// Gets the ETag value for this resource.
        /// </summary>
        /// <value>
        /// A string representing the current version of the resource.
        /// </value>
        public string ETag => _eTag;

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// Creates a new serialized instance from the provided resource.
        /// </summary>
        /// <typeparam name="TSerialized">The specific serialized class type to create.</typeparam>
        /// <param name="resource">The resource to serialize.</param>
        /// <returns>
        /// A new instance of <typeparamref name="TSerialized"/> containing the serialized resource.
        /// </returns>
        protected static TSerialized BaseCreate<TSerialized>(
            T resource) where TSerialized : BaseSerialized<T>, new()
        {
            // Create an instance of TSerialized
            var serialized = new TSerialized()
            {
                // Serialize the resource to a JSON string
                _jsonString = JsonSerializer.Serialize(resource, _options),

                // Create a new ETag for optimistic concurrency
                _eTag = Guid.NewGuid().ToString(),
            };

            return serialized;
        }

        #endregion
    }

    /// <summary>
    /// Represents an item that has been serialized to a JSON string for storage.
    /// </summary>
    private class SerializedItem : BaseSerialized<TItem>
    {
        #region Public Static Methods

        /// <summary>
        /// Creates a new serialized item instance.
        /// </summary>
        /// <param name="item">The item to serialize.</param>
        /// <returns>
        /// A new <see cref="SerializedItem"/> containing the serialized item.
        /// </returns>
        public static SerializedItem Create(
            TItem item) => BaseCreate<SerializedItem>(item);

        #endregion
    }

    /// <summary>
    /// Represents an event that has been serialized to a JSON string for storage.
    /// </summary>
    private class SerializedEvent : BaseSerialized<ItemEvent<TItem>>
    {
        #region Public Static Methods

        /// <summary>
        /// Creates a new serialized event instance.
        /// </summary>
        /// <param name="itemEvent">The item event to serialize.</param>
        /// <returns>
        /// A new <see cref="SerializedEvent"/> containing the serialized event.
        /// </returns>
        public static SerializedEvent Create(
            ItemEvent<TItem> itemEvent) => BaseCreate<SerializedEvent>(itemEvent);

        #endregion
    }

    /// <summary>
    /// An in-memory data store that contains items and their associated events.
    /// </summary>
    /// <remarks>
    /// This class implements <see cref="IEnumerable{TItem}"/> to support LINQ queries
    /// against the in-memory data.
    /// </remarks>
    private class InMemoryStore : IEnumerable<TItem>
    {
        #region Private Fields

        /// <summary>
        /// The backing store of serialized items, keyed by "partitionKey:id".
        /// </summary>
        private readonly Dictionary<string, SerializedItem> _items = [];

        /// <summary>
        /// The backing store of serialized events in chronological order.
        /// </summary>
        private readonly List<SerializedEvent> _events = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryStore"/> class.
        /// </summary>
        /// <param name="store">Optional existing store to copy data from.</param>
        /// <remarks>
        /// If <paramref name="store"/> is provided, this constructor creates a deep copy
        /// of its items and events for batch transaction support.
        /// </remarks>
        public InMemoryStore(
            InMemoryStore? store = null)
        {
            if (store is not null)
            {
                _items = new Dictionary<string, SerializedItem>(store._items);
                _events = new List<SerializedEvent>(store._events);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new item in the backing data store.
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="itemEvent">The event that represents information about the creation operation.</param>
        /// <returns>
        /// The created item with its assigned ETag.
        /// </returns>
        /// <exception cref="CommandException">
        /// Thrown with <see cref="HttpStatusCode.Conflict"/> if an item with the same key already exists.
        /// </exception>
        public TItem CreateItem(
            TItem item,
            ItemEvent<TItem> itemEvent)
        {
            // Generate the composite key for this item
            var itemKey = InMemoryCommandProvider<TInterface, TItem>.GetItemKey(item);

            // Serialize the item and attempt to add it to the backing store
            var serializedItem = SerializedItem.Create(item);
            if (_items.TryAdd(itemKey, serializedItem) is false)
            {
                throw new CommandException(HttpStatusCode.Conflict);
            }

            // Serialize the event and add to the event log
            var serializedEvent = SerializedEvent.Create(itemEvent);
            _events.Add(serializedEvent);

            // Return the item with its assigned ETag
            return serializedItem.Resource;
        }

        /// <summary>
        /// Reads an item from the backing data store.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <param name="partitionKey">The partition key of the item.</param>
        /// <returns>
        /// The requested item if found; otherwise, <see langword="null"/>.
        /// </returns>
        public TItem? ReadItem(
            string id,
            string partitionKey)
        {
            // Generate the composite key for this item
            var itemKey = GetItemKey(
                partitionKey: partitionKey,
                id: id);

            // Attempt to retrieve the item from the backing store
            if (_items.TryGetValue(itemKey, out var serializedItem) is false)
            {
                // Item not found
                return null;
            }

            // Return the deserialized item
            return serializedItem.Resource;
        }

        /// <summary>
        /// Updates an existing item in the backing data store.
        /// </summary>
        /// <param name="item">The item to update with new values.</param>
        /// <param name="itemEvent">The event that represents information about the update operation.</param>
        /// <returns>
        /// The updated item with its new ETag.
        /// </returns>
        /// <exception cref="CommandException">
        /// Thrown with <see cref="HttpStatusCode.NotFound"/> if the item does not exist,
        /// or with <see cref="HttpStatusCode.Conflict"/> if the item's ETag does not match the current version.
        /// </exception>
        public TItem UpdateItem(
            TItem item,
            ItemEvent<TItem> itemEvent)
        {
            // Generate the composite key for this item
            var itemKey = InMemoryCommandProvider<TInterface, TItem>.GetItemKey(item);

            // Check if the item exists in the backing store
            if (_items.TryGetValue(itemKey, out var serializedItem) is false)
            {
                // not found
                throw new CommandException(HttpStatusCode.NotFound);
            }

            // Check if the ETag matches (optimistic concurrency check)
            if (string.Equals(serializedItem.ETag, item.ETag) is false)
            {
                throw new CommandException(HttpStatusCode.PreconditionFailed);
            }

            // Serialize the updated item and replace the existing one
            serializedItem = SerializedItem.Create(item);
            _items[itemKey] = serializedItem;

            // Serialize the event and add to the event log
            var serializedEvent = SerializedEvent.Create(itemEvent);
            _events.Add(serializedEvent);

            // Return the updated item with its new ETag
            return serializedItem.Resource;
        }

        /// <summary>
        /// Gets all events stored in the backing store.
        /// </summary>
        /// <returns>
        /// An array of all item events.
        /// </returns>
        public ItemEvent<TItem>[] GetEvents()
        {
            return _events.Select(se => se.Resource).ToArray();
        }

        #endregion

        #region IEnumerable Implementation

        /// <summary>
        /// Returns an enumerator that iterates through all items in the store.
        /// </summary>
        /// <returns>
        /// An enumerator for all items in the store.
        /// </returns>
        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (var serializedItem in _items.Values)
            {
                yield return serializedItem.Resource;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through all items in the store.
        /// </summary>
        /// <returns>
        /// An enumerator for all items in the store.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion
}
