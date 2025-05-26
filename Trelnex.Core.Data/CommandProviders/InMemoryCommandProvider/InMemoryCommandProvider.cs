using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// In-memory implementation of <see cref="CommandProvider{TInterface, TItem}"/>.
/// </summary>
/// <typeparam name="TInterface">Interface type.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Simulates a database-backed provider.
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
    /// Reader-writer lock for thread safety.
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// In-memory storage.
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
    /// Clears all data.
    /// </summary>
    /// <remarks>
    /// Used for testing.
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
    /// Gets all stored events.
    /// </summary>
    /// <returns>Array of all events.</returns>
    /// <remarks>
    /// Used for testing and debugging.
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
    /// Generates composite key.
    /// </summary>
    /// <param name="item">Item to key.</param>
    /// <returns>Key in format "partitionKey:id".</returns>
    private static string GetItemKey(
        BaseItem item)
    {
        return GetItemKey(
            partitionKey: item.PartitionKey,
            id: item.Id);
    }

    /// <summary>
    /// Generates composite key from parts.
    /// </summary>
    /// <param name="partitionKey">Partition key.</param>
    /// <param name="id">Item id.</param>
    /// <returns>Key in format "partitionKey:id".</returns>
    private static string GetItemKey(
        string partitionKey,
        string id)
    {
        return $"{partitionKey}:{id}";
    }

    /// <summary>
    /// Saves item to specified store.
    /// </summary>
    /// <param name="store">Store to save to.</param>
    /// <param name="request">Save request.</param>
    /// <returns>Saved item.</returns>
    /// <exception cref="InvalidOperationException">When SaveAction not recognized.</exception>
    /// <exception cref="CommandException">When storage operation fails.</exception>
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
    /// Base class for serialized items and events.
    /// </summary>
    /// <typeparam name="T">Item type being serialized.</typeparam>
    /// <remarks>
    /// Handles serialization/deserialization.
    /// </remarks>
    private abstract class BaseSerialized<T> where T : BaseItem
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
        /// Creates new serialized instance.
        /// </summary>
        /// <typeparam name="TSerialized">Serialized class type to create.</typeparam>
        /// <param name="resource">Resource to serialize.</param>
        /// <returns>New serialized instance.</returns>
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
    /// Serialized item.
    /// </summary>
    private class SerializedItem : BaseSerialized<TItem>
    {
        #region Public Static Methods

        /// <summary>
        /// Creates serialized item instance.
        /// </summary>
        /// <param name="item">Item to serialize.</param>
        /// <returns>New serialized item.</returns>
        public static SerializedItem Create(
            TItem item) => BaseCreate<SerializedItem>(item);

        #endregion
    }

    /// <summary>
    /// Serialized event.
    /// </summary>
    private class SerializedEvent : BaseSerialized<ItemEvent<TItem>>
    {
        #region Public Static Methods

        /// <summary>
        /// Creates serialized event instance.
        /// </summary>
        /// <param name="itemEvent">Event to serialize.</param>
        /// <returns>New serialized event.</returns>
        public static SerializedEvent Create(
            ItemEvent<TItem> itemEvent) => BaseCreate<SerializedEvent>(itemEvent);

        #endregion
    }

    /// <summary>
    /// In-memory data store.
    /// </summary>
    /// <remarks>
    /// Implements IEnumerable to support LINQ queries.
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
        /// Initializes a new store instance.
        /// </summary>
        /// <param name="store">Optional existing store to copy.</param>
        /// <remarks>
        /// Creates deep copy when store is provided.
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
        /// Creates new item.
        /// </summary>
        /// <param name="item">Item to create.</param>
        /// <param name="itemEvent">Creation event information.</param>
        /// <returns>Created item.</returns>
        /// <exception cref="CommandException">When item with same key exists.</exception>
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
        /// Updates existing item.
        /// </summary>
        /// <param name="item">Item with new values.</param>
        /// <param name="itemEvent">Update event information.</param>
        /// <returns>Updated item.</returns>
        /// <exception cref="CommandException">
        /// NotFound if item doesn't exist or PreconditionFailed if ETag mismatch.
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
        /// Gets all stored events.
        /// </summary>
        /// <returns>Array of all events.</returns>
        public ItemEvent<TItem>[] GetEvents()
        {
            return _events.Select(se => se.Resource).ToArray();
        }

        #endregion

        #region IEnumerable Implementation

        /// <summary>
        /// Returns enumerator for all items.
        /// </summary>
        /// <returns>Item enumerator.</returns>
        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (var serializedItem in _items.Values)
            {
                yield return serializedItem.Resource;
            }
        }

        /// <summary>
        /// Returns enumerator for all items.
        /// </summary>
        /// <returns>Item enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion
}
