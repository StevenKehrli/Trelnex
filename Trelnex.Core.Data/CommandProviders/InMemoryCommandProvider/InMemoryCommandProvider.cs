using System.Collections;
using System.Linq.Expressions;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

namespace Trelnex.Core.Data;

/// <summary>
/// An implementation of <see cref="ICommandProvider{TInterface, TItem}"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is a temporary store in memory for item storage and retrieval.
/// </para>
/// <para>
/// This command provider will serialize the item to a string for storage and deserialize the string to a item for retrieval.
/// This validates that the item is json attributed correctly for a persistent backing store (Cosmos).
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
    /// <summary>
    /// An exclusive lock to ensure that only one operation that modifies the backing store is in progress at a time
    /// </summary>
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// The in memory backing store;
    /// </summary>
    private InMemoryStore _store = new();

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
            // lock
            _lock.EnterReadLock();

            // read from the backing store
            var read = _store.ReadItem(id, partitionKey);

            // return
            return await Task.FromResult<TItem?>(read);
        }
        finally
        {
            // unlock
            _lock.ExitReadLock();
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

        // lock
        _lock.EnterWriteLock();

        // create a copy of the existing backing store to use for the batch
        var batchStore = new InMemoryStore(_store);

        // enumerate each item
        var saveRequestIndex = 0;
        for ( ; saveRequestIndex < requests.Length; saveRequestIndex++)
        {
            var saveRequest = requests[saveRequestIndex];

            try
            {
                // save the item
                var saved = SaveItem(batchStore, saveRequest);

                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        HttpStatusCode.OK,
                        saved);
            }
            catch (Exception ex) when (ex is CommandException || ex is InvalidOperationException)
            {
                // set the result to the exception status code
                var httpStatusCode = ex is CommandException commandEx
                    ? commandEx.HttpStatusCode
                    : HttpStatusCode.InternalServerError;

                saveResults[saveRequestIndex] =
                    new SaveResult<TInterface, TItem>(
                        httpStatusCode,
                        null);

                break;
            }
        }

        if (saveRequestIndex == requests.Length)
        {
            // the batch completed successfully, update the backing store
            _store = batchStore;
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

        // unlock
        _lock.ExitWriteLock();

        return await Task.FromResult(saveResults);
    }

    /// <summary>
    /// Create the <see cref="IQueryable{TItem}"/> to query the items.
    /// </summary>
    /// <returns></returns>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // deferred execution, so do not need to lock
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Execute the query specified by the <see cref="IQueryable{TItem}"/> and return the results as an async enumerable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The <see cref="IAsyncEnumerable{TInterface}"/>.</returns>
    protected override IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // lock
            _lock.EnterReadLock();

            // convert the queryable to use _store instead of EmptyPartition
            var mce = (queryable.Expression as MethodCallExpression)!;

            var constantValue = _store.AsQueryable();
            var constantExpression = Expression.Constant(constantValue);

            var methodCallExpression = Expression.Call(
                mce.Method,
                constantExpression,
                mce.Arguments[1]!);

            // create the query from the store and the method call expression
            var queryableFromExpression = _store
                .AsQueryable()
                .Provider
                .CreateQuery<TItem>(methodCallExpression);

            foreach (var item in queryableFromExpression.AsEnumerable())
            {
                yield return item;
            }
        }
        finally
        {
            // unlock
            _lock.ExitReadLock();
        }
    }

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

    private static string GetItemKey(
        BaseItem item)
    {
        return GetItemKey(
            partitionKey: item.PartitionKey,
            id: item.Id);
    }

    private static string GetItemKey(
        string partitionKey,
        string id)
    {
        return $"{partitionKey}:{id}";
    }

    /// <summary>
    /// Save the item to the backing store.
    /// </summary>
    /// <param name="store">The backing store to save the item to.</param>
    /// <param name="request">The save request with item and event to save.</param>
    /// <returns>The result of the save operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="SaveAction"/> is not recognized.</exception>
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

    /// <summary>
    /// Represents an item or event that has been serialized to a json string.
    /// </summary>
    /// <typeparam name="T">The type of the item or event.</typeparam>
    private class BaseSerialized<T> where T : BaseItem
    {
        /// <summary>
        /// The json serializer options
        /// </summary>
        private static readonly JsonSerializerOptions _options = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private string _jsonString = null!;
        private string _eTag = null!;

        /// <summary>
        /// Gets the resource.
        /// </summary>
        public T Resource
        {
            get
            {
                // deserialize to the resource
                var resource = JsonSerializer.Deserialize<T>(_jsonString, _options)!;

                // set the etag
                resource.ETag = _eTag;

                // return the resource
                return resource;
            }
        }

        /// <summary>
        /// Gets the ETag of the resource.
        /// </summary>
        public string ETag => _eTag;

        protected static TSerialized BaseCreate<TSerialized>(
            T resource) where TSerialized : BaseSerialized<T>, new()
        {
            // create an instance of TSerialized
            var serialized = new TSerialized()
            {
                // serialize to a json string
                _jsonString = JsonSerializer.Serialize(resource, _options),

                // create a new ETag
                _eTag = Guid.NewGuid().ToString(),
            };

            return serialized;
        }
    }

    /// <summary>
    /// Represents an item that has been serialized to a json string.
    /// </summary>
    /// <param name="jsonString">The serialized json string of the item.</param>
    /// <param name="eTag">The ETag of the item.</param>
    private class SerializedItem
        : BaseSerialized<TItem>
    {
        public static SerializedItem Create(
            TItem item) => BaseCreate<SerializedItem>(item);
    }

    /// <summary>
    /// Represents an event that has been serialized to a json string.
    /// </summary>
    private class SerializedEvent
        : BaseSerialized<ItemEvent<TItem>>
    {
        public static SerializedEvent Create(
            ItemEvent<TItem> itemEvent) => BaseCreate<SerializedEvent>(itemEvent);
    }

    private class InMemoryStore : IEnumerable<TItem>
    {
        /// <summary>
        /// The backing store of items
        /// </summary>
        private readonly Dictionary<string, SerializedItem> _items = [];

        /// <summary>
        /// The backing store of events
        /// </summary>
        private readonly List<SerializedEvent> _events = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryStore"/> class.
        /// </summary>
        /// <param name="store">The store to copy from.</param>
        public InMemoryStore(
            InMemoryStore? store = null)
        {
            if (store is not null)
            {
                _items = new Dictionary<string, SerializedItem>(store._items);
                _events = new List<SerializedEvent>(store._events);
            }
        }

        /// <summary>
        /// Creates a item in the backing data store.
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="itemEvent">The <see cref="ItemEvent"> that represents information regarding the item and the caller that invoked the save method.</param>
        /// <returns>The item that was created.</returns>
        public TItem CreateItem(
            TItem item,
            ItemEvent<TItem> itemEvent)
        {
            // get the item key
            var itemKey = InMemoryCommandProvider<TInterface, TItem>.GetItemKey(item);

            // serialize the item and add to the backing store
            var serializedItem = SerializedItem.Create(item);
            if (_items.TryAdd(itemKey, serializedItem) is false)
            {
                throw new CommandException(HttpStatusCode.Conflict);
            }

            // serialize the event and add to the backing store
            var serializedEvent = SerializedEvent.Create(itemEvent);
            _events.Add(serializedEvent);

            return serializedItem.Resource;
        }

        /// <summary>
        /// Reads a item from the backing data store.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <param name="partitionKey">The partition key of the item.</param>
        /// <returns>The item that was read.</returns>
        public TItem? ReadItem(
            string id,
            string partitionKey)
        {
            // get the item key
            var itemKey = GetItemKey(
                partitionKey: partitionKey,
                id: id);

            // get the item
            if (_items.TryGetValue(itemKey, out var serializedItem) is false)
            {
                // not found
                return null;
            }

            // return
            return serializedItem.Resource;
        }

        /// <summary>
        /// Updates an item in the backing data store.
        /// </summary>
        /// <param name="item">The item to update.</param>
        /// <param name="itemEvent">The <see cref="ItemEvent"> that represents information regarding the item and the caller that invoked the save method.</param>
        /// <returns>The item that was updated.</returns>
        public TItem UpdateItem(
            TItem item,
            ItemEvent<TItem> itemEvent)
        {
            // get the item key
            var itemKey = InMemoryCommandProvider<TInterface, TItem>.GetItemKey(item);

            // get the item
            if (_items.TryGetValue(itemKey, out var serializedItem) is false)
            {
                // not found
                throw new CommandException(HttpStatusCode.NotFound);
            }

            // check the version (ETag) is unchanged
            if (string.Equals(serializedItem.ETag, item.ETag) is false)
            {
                throw new CommandException(HttpStatusCode.Conflict);
            }

            // serialize the item and update in the backing store
            serializedItem = SerializedItem.Create(item);
            _items[itemKey] = serializedItem;

            // serialize the event and add to the backing store
            var serializedEvent = SerializedEvent.Create(itemEvent);
            _events.Add(serializedEvent);

            // return
            return serializedItem.Resource;
        }

        public ItemEvent<TItem>[] GetEvents()
        {
            return _events.Select(se => se.Resource).ToArray();
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            foreach (var serializedItem in _items.Values)
            {
                yield return serializedItem.Resource;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
