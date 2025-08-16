using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// In-memory implementation of data provider for testing and development scenarios.
/// </summary>
/// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
/// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
/// <remarks>
/// Provides thread-safe, non-persistent data storage with full CRUD operations and LINQ query support.
/// Simulates database behavior including optimistic concurrency control via ETags.
/// </remarks>
internal class InMemoryDataProvider<TInterface, TItem>
    : DataProvider<TInterface, TItem>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Fields

    /// <summary>
    /// Reader-writer lock providing thread-safe access to the in-memory store.
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// In-memory data store containing serialized items and events.
    /// </summary>
    private InMemoryStore _store;

    #endregion

    #region Constructor

    public InMemoryDataProvider(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        IBlockCipherService? blockCipherService = null)
        : base(typeName, itemValidator, commandOperations, blockCipherService)
    {
        _store = CreateStore();
    }

    #endregion

    #region Protected Methods

    /// <inheritdoc/>
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

    /// <inheritdoc/>
#pragma warning disable CS1998, CS8425
    protected override async IAsyncEnumerable<TItem> ExecuteQueryableAsync(
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
#pragma warning restore CS1998, CS8425

    /// <inheritdoc/>
#pragma warning disable CS1998
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
            return read;
        }
        finally
        {
            // Always release the lock, even if an exception occurs
            _lock.ExitReadLock();
        }
    }
#pragma warning restore CS1998

    /// <inheritdoc/>
#pragma warning disable CS1998
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
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

        return saveResults;
    }
#pragma warning restore CS1998

    #endregion

    #region Internal Methods

    /// <summary>
    /// Clears all stored data and events from the in-memory store.
    /// </summary>
    /// <remarks>
    /// Thread-safe operation intended for testing scenarios to reset provider state.
    /// </remarks>
    internal void Clear()
    {
        try
        {
            _lock.EnterWriteLock();

            _store = CreateStore();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Retrieves all stored events for debugging and testing purposes.
    /// </summary>
    /// <returns>An array containing all events that have occurred in this provider instance.</returns>
    /// <remarks>
    /// Thread-safe read operation that provides insight into the complete audit trail.
    /// </remarks>
    internal ItemEvent[] GetEvents()
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
    /// Persists an item to the specified store based on the save action type.
    /// </summary>
    /// <param name="store">The target store for the save operation.</param>
    /// <param name="request">The save request containing item data and action type.</param>
    /// <returns>The successfully saved item with updated metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the SaveAction is not recognized.</exception>
    /// <exception cref="CommandException">Thrown when the storage operation fails due to conflicts or constraints.</exception>
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

    #region Private Methods

    private InMemoryStore CreateStore()
    {
        return new InMemoryStore(
            serializeItem: SerializeItemToNode,
            serializeEvent: SerializeEventToNode,
            deserializeItem: DeserializeItem);
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Thread-safe in-memory data store supporting CRUD operations and LINQ queries.
    /// </summary>
    /// <remarks>
    /// Implements optimistic concurrency control and maintains audit trail through events.
    /// Supports deep copying for transactional batch operations.
    /// </remarks>
    private class InMemoryStore : IEnumerable<TItem>
    {
        #region Private Fields

        /// <summary>
        /// Items dictionary.
        /// </summary>
        private readonly Dictionary<string, SerializedResource> _items;

        /// <summary>
        /// Events list.
        /// </summary>
        private readonly List<SerializedResource> _events;

        /// <summary>
        /// Function to serialize an item.
        /// </summary>
        private readonly Func<TItem, JsonNode> _serializeItem;

        /// <summary>
        /// Function to serialize an event.
        /// </summary>
        private readonly Func<ItemEvent, JsonNode> _serializeEvent;

        /// <summary>
        /// Function to deserialize an item.
        /// </summary>
        private readonly Func<string, TItem?> _deserializeItem;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new store instance
        /// </summary>
        /// <param name="serializeItem">Function to serialize an item.</param>]
        /// <param name="serializeEvent">Function to serialize an event.</param>
        /// <param name="deserializeItem">Function to deserialize an item.</param>
        public InMemoryStore(
            Func<TItem, JsonNode> serializeItem,
            Func<ItemEvent, JsonNode> serializeEvent,
            Func<string, TItem?> deserializeItem)
        {
            _serializeItem = serializeItem;
            _serializeEvent = serializeEvent;
            _deserializeItem = deserializeItem;

            _items = [];
            _events = [];
        }

        /// <summary>
        /// Initializes a new store instance, copying from an existing store.
        /// </summary>
        /// <param name="store">Existing store to deep copy from for transactional operations.</param>
        public InMemoryStore(
            InMemoryStore store)
        {
            _serializeItem = store._serializeItem;
            _serializeEvent = store._serializeEvent;
            _deserializeItem = store._deserializeItem;

            _items = new Dictionary<string, SerializedResource>(store._items);
            _events = new List<SerializedResource>(store._events);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new item in the store with conflict detection.
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="itemEvent">The creation event for audit trail.</param>
        /// <returns>The created item with assigned ETag.</returns>
        /// <exception cref="CommandException">Thrown when an item with the same key already exists.</exception>
        public TItem CreateItem(
            TItem item,
            ItemEvent itemEvent)
        {
            // Generate a new eTag for this item
            var eTag = Guid.NewGuid().ToString();

            // Generate the composite key for this item
            var itemKey = GetItemKey(
                partitionKey: item.PartitionKey,
                id: item.Id);

            // Serialize the item and attempt to add it to the backing store
            var serializedItem = SerializeItem(item, eTag);
            if (_items.TryAdd(itemKey, serializedItem) is false)
            {
                throw new CommandException(HttpStatusCode.Conflict);
            }

            // Serialize the event and add to the event log
            var serializedEvent = SerializeEvent(itemEvent, eTag);
            _events.Add(serializedEvent);

            // Return the item with its assigned ETag
            return DeserializeItem(serializedItem)!;
        }

        /// <summary>
        /// Reads item.
        /// </summary>
        /// <param name="id">Item identifier.</param>
        /// <param name="partitionKey">Partition key.</param>
        /// <returns>Requested item or null if not found.</returns>
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
            return DeserializeItem(serializedItem);
        }

        /// <summary>
        /// Updates an existing item with optimistic concurrency control.
        /// </summary>
        /// <param name="item">The item with updated values and current ETag.</param>
        /// <param name="itemEvent">The update event for audit trail.</param>
        /// <returns>The updated item with new ETag.</returns>
        /// <exception cref="CommandException">
        /// Thrown when item is not found (NotFound) or ETag doesn't match (PreconditionFailed).
        /// </exception>
        public TItem UpdateItem(
            TItem item,
            ItemEvent itemEvent)
        {
            // Generate the composite key for this item
            var itemKey = GetItemKey(
                partitionKey: item.PartitionKey,
                id: item.Id);

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

            // Generate a new eTag for this item
            var eTag = Guid.NewGuid().ToString();

            // Serialize the updated item and replace the existing one
            serializedItem = SerializeItem(item, eTag);
            _items[itemKey] = serializedItem;

            // Serialize the event and add to the event log
            var serializedEvent = SerializeEvent(itemEvent, eTag);
            _events.Add(serializedEvent);

            // Return the updated item with its new ETag
            return DeserializeItem(serializedItem);
        }

        /// <summary>
        /// Retrieves all stored events for audit and debugging purposes.
        /// </summary>
        /// <returns>An array of all events in chronological order.</returns>
        public ItemEvent[] GetEvents()
        {
            return _events.Select(DeserializeEvent).ToArray();
        }

        #endregion

        #region IEnumerable Implementation

        /// <summary>
        /// Returns an enumerator for all items in the store.
        /// </summary>
        /// <returns>An enumerator that iterates through all stored items.</returns>
        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (var serializedItem in _items.Values)
            {
                yield return DeserializeItem(serializedItem);
            }
        }

        /// <summary>
        /// Returns a non-generic enumerator for all items in the store.
        /// </summary>
        /// <returns>A non-generic enumerator that iterates through all stored items.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Deserializes an item event.
        /// </summary>
        /// <param name="serializedEvent">The serialized event to deserialize.</param>
        /// <remarks>
        /// This method deserializes the event using the default JsonSerializer serializer.
        /// This ensures the event is deserialized to represent how the event is persisted in the store.
        /// Normally we do not deserialize (read) events from the store, but this method is provided for testing purposes.
        /// </remarks>
        /// <returns>The deserialized item event.</returns>
        private static ItemEvent DeserializeEvent(
            SerializedResource serializedEvent)
        {
            return JsonSerializer.Deserialize<ItemEvent>(serializedEvent.JsonString)!;
        }

        /// <summary>
        /// Generates a composite key from separate partition key and identifier components.
        /// </summary>
        /// <param name="partitionKey">The partition key component.</param>
        /// <param name="id">The identifier component.</param>
        /// <returns>A composite key in the format "partitionKey:id".</returns>
        private static string GetItemKey(
            string partitionKey,
            string id)
        {
            return $"{partitionKey}:{id}";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Deserializes a serialized item.
        /// </summary>
        /// <param name="serializedItem">The serialized item to deserialize.</param>
        /// <remarks>
        /// This method deserializes the item using the DataProvider serializer.
        /// This ensures the item is properly deserialized.
        /// </remarks>
        /// <returns>The deserialized item.</returns>
        private TItem DeserializeItem(
            SerializedResource serializedItem)
        {
            return _deserializeItem(serializedItem.JsonString)!;
        }

        /// <summary>
        /// Serializes an item event.
        /// </summary>
        /// <param name="itemEvent">The item event to serialize.</param>
        /// <param name="eTag">The ETag for the item event.</param>
        /// <remarks>
        /// This method serializes the item event using the DataProvider serializer.
        /// This ensures the event is properly serialized.
        /// </remarks>
        /// <returns>The serialized item event.</returns>
        private SerializedResource SerializeEvent(
            ItemEvent itemEvent,
            string eTag)
        {
            var jsonNode = _serializeEvent(itemEvent);
            var serializedEvent = new SerializedResource(jsonNode)
            {
                ETag = eTag
            };

            return serializedEvent;
        }

        /// <summary>
        /// Serializes an item.
        /// </summary>
        /// <param name="item">The item to serialize.</param>
        /// <param name="eTag">The ETag for the item.</param>
        /// <remarks>
        /// This method serializes the item event using the DataProvider serializer.
        /// This ensures the item is properly serialized.
        /// </remarks>
        /// <returns>The serialized item.</returns>
        private SerializedResource SerializeItem(
            TItem item,
            string eTag)
        {
            var jsonNode = _serializeItem(item);
            var serializedItem = new SerializedResource(jsonNode)
            {
                ETag = eTag
            };

            return serializedItem;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Class for serialized resources.
        /// </summary>
        private class SerializedResource(
            JsonNode jsonNode)
        {
            #region Private Static Fields

            /// <summary>
            /// The json property name of the ETag property.
            /// </summary>
            private static readonly string _eTagPropertyName = GetJsonPropertyName(nameof(BaseItem.ETag));

            #endregion

            #region Protected Fields

            protected JsonNode _jsonNode = jsonNode;

            #endregion

            #region Public Properties

            /// <summary>
            /// The ETag value for optimistic concurrency control.
            /// </summary>
            public string ETag
            {
                get => _jsonNode[_eTagPropertyName]?.GetValue<string>() ?? null!;
                set => _jsonNode[_eTagPropertyName] = value;
            }

            /// <summary>
            /// Converts the resource to a JSON string.
            /// </summary>
            /// <returns></returns>
            public string JsonString => _jsonNode.ToJsonString();

            #endregion

            #region Private Static Methods

            /// <summary>
            /// Gets the json property name for the specified property.
            /// </summary>
            /// <returns>The json property name for the specified property.</returns>
            private static string GetJsonPropertyName(
                string propertyName)
            {
                // Use reflection to get the json property name dynamically
                return typeof(BaseItem)
                    .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
                    .GetCustomAttribute<JsonPropertyNameAttribute>()!
                    .Name;
            }

            #endregion
        }

        #endregion
    }

    #endregion
}
