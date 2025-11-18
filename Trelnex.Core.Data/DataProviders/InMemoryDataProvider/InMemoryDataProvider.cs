using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Encryption;

namespace Trelnex.Core.Data;

/// <summary>
/// In-memory data provider implementation for testing and development purposes.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
internal class InMemoryDataProvider<TItem>
    : DataProvider<TItem>
    where TItem : BaseItem, new()
{
    #region Private Fields

    // Reader-writer lock for thread-safe access to the store
    private readonly ReaderWriterLockSlim _lock = new();

    // In-memory storage for items and events
    private InMemoryStore _store;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDataProvider{TItem}"/> class.
    /// </summary>
    /// <param name="typeName">The name of the item type.</param>
    /// <param name="itemValidator">Optional validator for domain-specific rules.</param>
    /// <param name="commandOperations">Allowed CRUD operations, defaults to Read-only.</param>
    /// <param name="eventPolicy">Optional event policy for change tracking.</param>
    /// <param name="blockCipherService">Optional service for encrypting sensitive data.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    private InMemoryDataProvider(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
        : base(
            typeName: typeName,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService,
            logger: logger)
    {
        _store = CreateStore();
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Creates an in-memory data provider for the specified item type.
    /// </summary>
    /// <param name="typeName">Type name identifier for the items.</param>
    /// <param name="itemValidator">Optional validator for items.</param>
    /// <param name="commandOperations">Allowed operations for this provider.</param>
    /// <param name="eventPolicy">Optional event policy for change tracking.</param>
    /// <param name="blockCipherService">Optional encryption service for sensitive data.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <returns>A configured in-memory data provider instance.</returns>
    public static Task<IDataProvider<TItem>> CreateAsync(
        string typeName,
        IValidator<TItem>? itemValidator = null,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
    {
        var provider = new InMemoryDataProvider<TItem>(
            typeName: typeName,
            itemValidator: itemValidator,
            commandOperations: commandOperations,
            eventPolicy: eventPolicy,
            blockCipherService: blockCipherService,
            logger: logger);
        
        return Task.FromResult(provider as IDataProvider<TItem>);
    }

    #endregion

    #region Protected Methods

    /// <inheritdoc/>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Return empty queryable with base filters - actual data substitution happens in ExecuteQueryableAsync
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
            // Acquire read lock for thread-safe access to store
            _lock.EnterReadLock();

            // Get the method call expression containing filter predicates
            var mce = (queryable.Expression as MethodCallExpression)!;

            // Replace empty source with actual store data
            var constantValue = _store.AsQueryable();
            var constantExpression = Expression.Constant(constantValue);

            // Build new expression with store as data source
            var methodCallExpression = Expression.Call(
                mce.Method,
                constantExpression,
                mce.Arguments[1]!);

            // Execute query against the store
            var queryableFromExpression = _store
                .AsQueryable()
                .Provider
                .CreateQuery<TItem>(methodCallExpression);

            // Return matching items
            foreach (var item in queryableFromExpression)
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return item;
            }
        }
        finally
        {
            // Always release read lock
            _lock.ExitReadLock();
        }
    }
#pragma warning restore CS1998, CS8425

    /// <inheritdoc/>
    protected override Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Acquire read lock for thread-safe access
            _lock.EnterReadLock();

            // Read item from store
            var read = _store.ReadItem(id, partitionKey);

            return Task.FromResult(read);
        }
        finally
        {
            // Always release read lock
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc/>
    protected override Task<SaveResult<TItem>[]> SaveBatchAsync(
        SaveRequest<TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Initialize results array
        var saveResults = new SaveResult<TItem>[requests.Length];

        // Acquire write lock for exclusive access
        _lock.EnterWriteLock();

        // Create working copy of store for transactional behavior
        var batchStore = new InMemoryStore(_store);

        // Process each request in the batch
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            var saveRequest = requests[saveRequestIndex];

            try
            {
                // Attempt to save item to working store
                var saved = SaveItem(batchStore, saveRequest);

                // Record success
                saveResults[saveRequestIndex] =
                    new SaveResult<TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (ex is CommandException || ex is InvalidOperationException)
            {
                // Handle save failure
                var httpStatusCode = ex is CommandException commandEx
                    ? commandEx.HttpStatusCode
                    : HttpStatusCode.InternalServerError;

                // Record failure
                saveResults[saveRequestIndex] =
                    new SaveResult<TItem>(
                        httpStatusCode,
                        null);

                // Stop processing on first failure
                break;
            }
        }

        // Apply transactional behavior
        if (saveRequestIndex == requests.Length)
        {
            // All requests succeeded - commit the working store
            _store = batchStore;
        }
        else
        {
            // Some request failed - mark remaining requests as dependent failures
            for (var saveResultIndex = 0; saveResultIndex < saveResults.Length; saveResultIndex++)
            {
                // Skip the failed request (keeps its original error)
                if (saveResultIndex == saveRequestIndex) continue;

                saveResults[saveResultIndex] =
                    new SaveResult<TItem>(
                        HttpStatusCode.FailedDependency,
                        null);
            }
        }

        // Always release write lock
        _lock.ExitWriteLock();

        return Task.FromResult(saveResults);
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Removes all items and events from the store.
    /// </summary>
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
    /// Returns all stored events for testing and debugging.
    /// </summary>
    /// <returns>Array of all events in the store.</returns>
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
    /// Saves an item to the specified store based on the save action type.
    /// </summary>
    /// <param name="store">Target store for the operation.</param>
    /// <param name="request">Save request containing item and action.</param>
    /// <returns>The saved item with updated metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown for unrecognized save actions.</exception>
    /// <exception cref="CommandException">Thrown for storage conflicts or constraints.</exception>
    private static TItem SaveItem(
        InMemoryStore store,
        SaveRequest<TItem> request) => request.SaveAction switch
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
    /// In-memory storage implementation with support for CRUD operations and LINQ queries.
    /// </summary>
    private class InMemoryStore : IEnumerable<TItem>
    {
        #region Private Fields

        // Dictionary storing serialized items by composite key
        private readonly Dictionary<string, SerializedResource> _items;

        // List storing serialized events in chronological order
        private readonly List<SerializedResource> _events;

        // Function to serialize items using data provider settings
        private readonly Func<TItem, JsonNode> _serializeItem;

        // Function to serialize events using data provider settings
        private readonly Func<ItemEvent, JsonNode> _serializeEvent;

        // Function to deserialize items using data provider settings
        private readonly Func<string, TItem?> _deserializeItem;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new empty store with serialization functions.
        /// </summary>
        /// <param name="serializeItem">Function to serialize items.</param>
        /// <param name="serializeEvent">Function to serialize events.</param>
        /// <param name="deserializeItem">Function to deserialize items.</param>
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
        /// Creates a copy of an existing store for transactional operations.
        /// </summary>
        /// <param name="store">Existing store to copy.</param>
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
        /// Creates a new item in the store.
        /// </summary>
        /// <param name="item">Item to create.</param>
        /// <param name="itemEvent">Event to record the creation.</param>
        /// <returns>Created item with assigned ETag.</returns>
        /// <exception cref="CommandException">Thrown if item already exists.</exception>
        public TItem CreateItem(
            TItem item,
            ItemEvent? itemEvent)
        {
            // Generate unique ETag for optimistic concurrency control
            var eTag = Guid.NewGuid().ToString();

            // Build composite key for item lookup
            var itemKey = GetItemKey(
                partitionKey: item.PartitionKey,
                id: item.Id);

            // Serialize and attempt to add item
            var serializedItem = SerializeItem(item, eTag);
            if (_items.TryAdd(itemKey, serializedItem) is false)
            {
                throw new CommandException(HttpStatusCode.Conflict);
            }

            // Record the creation event
            if (itemEvent is not null)
            {
                var serializedEvent = SerializeEvent(itemEvent, eTag);
                _events.Add(serializedEvent);
            }

            // Return item with assigned ETag
            return DeserializeItem(serializedItem)!;
        }

        /// <summary>
        /// Retrieves an item by its identifiers.
        /// </summary>
        /// <param name="id">Item identifier.</param>
        /// <param name="partitionKey">Partition key.</param>
        /// <returns>Item if found, null otherwise.</returns>
        public TItem? ReadItem(
            string id,
            string partitionKey)
        {
            // Build composite key for lookup
            var itemKey = GetItemKey(
                partitionKey: partitionKey,
                id: id);

            // Try to find item in store
            if (_items.TryGetValue(itemKey, out var serializedItem) is false)
            {
                return null;
            }

            // Deserialize and return item
            return DeserializeItem(serializedItem);
        }

        /// <summary>
        /// Updates an existing item with optimistic concurrency control.
        /// </summary>
        /// <param name="item">Item with updated values and current ETag.</param>
        /// <param name="itemEvent">Event to record the update.</param>
        /// <returns>Updated item with new ETag.</returns>
        /// <exception cref="CommandException">Thrown if item not found or ETag mismatch.</exception>
        public TItem UpdateItem(
            TItem item,
            ItemEvent? itemEvent)
        {
            // Build composite key for lookup
            var itemKey = GetItemKey(
                partitionKey: item.PartitionKey,
                id: item.Id);

            // Check if item exists
            if (_items.TryGetValue(itemKey, out var serializedItem) is false)
            {
                throw new CommandException(HttpStatusCode.NotFound);
            }

            // Verify ETag for optimistic concurrency
            if (string.Equals(serializedItem.ETag, item.ETag) is false)
            {
                throw new CommandException(HttpStatusCode.PreconditionFailed);
            }

            // Generate new ETag
            var eTag = Guid.NewGuid().ToString();

            // Serialize and update item
            serializedItem = SerializeItem(item, eTag);
            _items[itemKey] = serializedItem;

            // Record the update event
            if (itemEvent is not null)
            {
                var serializedEvent = SerializeEvent(itemEvent, eTag);
                _events.Add(serializedEvent);
            }

            // Return updated item with new ETag
            return DeserializeItem(serializedItem);
        }

        /// <summary>
        /// Returns all stored events.
        /// </summary>
        /// <returns>Array of all events in chronological order.</returns>
        public ItemEvent[] GetEvents()
        {
            return _events.Select(DeserializeEvent).ToArray();
        }

        #endregion

        #region IEnumerable Implementation

        /// <summary>
        /// Returns an enumerator for all items in the store.
        /// </summary>
        /// <returns>Enumerator for stored items.</returns>
        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (var serializedItem in _items.Values)
            {
                yield return DeserializeItem(serializedItem);
            }
        }

        /// <summary>
        /// Returns a non-generic enumerator for all items.
        /// </summary>
        /// <returns>Non-generic enumerator for stored items.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Deserializes an event from its serialized form.
        /// </summary>
        /// <param name="serializedEvent">Serialized event to deserialize.</param>
        /// <returns>Deserialized event.</returns>
        private static ItemEvent DeserializeEvent(
            SerializedResource serializedEvent)
        {
            return JsonSerializer.Deserialize<ItemEvent>(serializedEvent.JsonString)!;
        }

        /// <summary>
        /// Creates a composite key from partition key and ID.
        /// </summary>
        /// <param name="partitionKey">Partition key component.</param>
        /// <param name="id">ID component.</param>
        /// <returns>Composite key in format "partitionKey:id".</returns>
        private static string GetItemKey(
            string partitionKey,
            string id)
        {
            return $"{partitionKey}:{id}";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Deserializes an item using the configured deserializer.
        /// </summary>
        /// <param name="serializedItem">Serialized item to deserialize.</param>
        /// <returns>Deserialized item.</returns>
        private TItem DeserializeItem(
            SerializedResource serializedItem)
        {
            return _deserializeItem(serializedItem.JsonString)!;
        }

        /// <summary>
        /// Serializes an event using the configured serializer.
        /// </summary>
        /// <param name="itemEvent">Event to serialize.</param>
        /// <param name="eTag">ETag to assign to the event.</param>
        /// <returns>Serialized event resource.</returns>
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
        /// Serializes an item using the configured serializer.
        /// </summary>
        /// <param name="item">Item to serialize.</param>
        /// <param name="eTag">ETag to assign to the item.</param>
        /// <returns>Serialized item resource.</returns>
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
        /// Represents a serialized resource with ETag support.
        /// </summary>
        private class SerializedResource(
            JsonNode jsonNode)
        {
            #region Private Static Fields

            // JSON property name for ETag field
            private static readonly string _eTagPropertyName = GetJsonPropertyName(nameof(BaseItem.ETag));

            #endregion

            #region Protected Fields

            protected JsonNode _jsonNode = jsonNode;

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets or sets the ETag value for optimistic concurrency control.
            /// </summary>
            public string ETag
            {
                get => _jsonNode[_eTagPropertyName]?.GetValue<string>() ?? null!;
                set => _jsonNode[_eTagPropertyName] = value;
            }

            /// <summary>
            /// Gets the JSON string representation of the resource.
            /// </summary>
            public string JsonString => _jsonNode.ToJsonString();

            #endregion

            #region Private Static Methods

            /// <summary>
            /// Gets the JSON property name for a given property using reflection.
            /// </summary>
            /// <param name="propertyName">Property name to look up.</param>
            /// <returns>JSON property name from JsonPropertyNameAttribute.</returns>
            private static string GetJsonPropertyName(
                string propertyName)
            {
                // Use reflection to get JSON property name from attribute
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
