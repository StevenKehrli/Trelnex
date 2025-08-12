using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

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
internal class InMemoryDataProvider<TInterface, TItem>(
    string typeName,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null)
    : DataProvider<TInterface, TItem>(typeName, itemValidator, commandOperations)
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
    private InMemoryStore _store = new();

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

            _store = new();
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
    /// Generates a composite key from an item's partition key and identifier.
    /// </summary>
    /// <param name="item">The item to generate a key for.</param>
    /// <returns>A composite key in the format "partitionKey:id".</returns>
    private static string GetItemKey(
        BaseItem item)
    {
        return GetItemKey(
            partitionKey: item.PartitionKey,
            id: item.Id);
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
        private readonly Dictionary<string, SerializedItem> _items = [];

        /// <summary>
        /// Events list.
        /// </summary>
        private readonly List<SerializedEvent> _events = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new store instance, optionally copying from an existing store.
        /// </summary>
        /// <param name="store">Optional existing store to deep copy from for transactional operations.</param>
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
            // Generate the composite key for this item
            var itemKey = InMemoryDataProvider<TInterface, TItem>.GetItemKey(item);

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
            return serializedItem.Resource;
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
            var itemKey = InMemoryDataProvider<TInterface, TItem>.GetItemKey(item);

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
        /// Retrieves all stored events for audit and debugging purposes.
        /// </summary>
        /// <returns>An array of all events in chronological order.</returns>
        public ItemEvent[] GetEvents()
        {
            return _events.Select(se => se.Resource).ToArray();
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
                yield return serializedItem.Resource;
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
    }

    /// <summary>
    /// Abstract base class for JSON serialization of items and events with ETag support.
    /// </summary>
    /// <typeparam name="T">The type being serialized, must inherit from BaseItem.</typeparam>
    /// <remarks>
    /// Provides consistent serialization behavior and optimistic concurrency control through ETags.
    /// </remarks>
    private abstract class SerializedBase<T> where T : BaseItem
    {
        #region Private Static Fields

        /// <summary>
        /// JSON serializer options.
        /// </summary>
        private static readonly JsonSerializerOptions _options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        #endregion

        #region Private Fields

        /// <summary>
        /// Serialized JSON string.
        /// </summary>
        private string _jsonString = null!;

        /// <summary>
        /// ETag for optimistic concurrency.
        /// </summary>
        private string _eTag = null!;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets deserialized resource.
        /// </summary>
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
        /// ETag value.
        /// </summary>
        public string ETag => _eTag;

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// Creates a new serialized instance from a resource with generated ETag.
        /// </summary>
        /// <typeparam name="TSerialized">The concrete serialized type to create.</typeparam>
        /// <param name="resource">The resource to serialize.</param>
        /// <returns>A new serialized instance with JSON data and unique ETag.</returns>
        protected static TSerialized CreateBase<TSerialized>(
            T resource) where TSerialized : SerializedBase<T>, new()
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
    /// Serialized representation of an event with JSON storage and ETag.
    /// </summary>
    private class SerializedEvent : SerializedBase<ItemEvent>
    {
        #region Public Static Methods

        /// <summary>
        /// Creates a serialized event instance from the provided event.
        /// </summary>
        /// <param name="itemEvent">The event to serialize.</param>
        /// <returns>A new serialized event with JSON representation and ETag.</returns>
        public static SerializedEvent Create(
            ItemEvent itemEvent) => CreateBase<SerializedEvent>(itemEvent);

        #endregion
    }

    /// <summary>
    /// Serialized representation of an item with JSON storage and ETag.
    /// </summary>
    private class SerializedItem : SerializedBase<TItem>
    {
        #region Public Static Methods

        /// <summary>
        /// Creates a serialized item instance from the provided item.
        /// </summary>
        /// <param name="item">The item to serialize.</param>
        /// <returns>A new serialized item with JSON representation and ETag.</returns>
        public static SerializedItem Create(
            TItem item) => CreateBase<SerializedItem>(item);

        #endregion
    }

    #endregion
}
