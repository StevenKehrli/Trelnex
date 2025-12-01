using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Encryption;
using Trelnex.Core.Json;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides base implementation for data access operations with validation and serialization.
/// </summary>
/// <typeparam name="TItem">The item type that extends BaseItem and has a parameterless constructor.</typeparam>
public abstract partial class DataProvider<TItem>
    : IDataProvider<TItem>
    where TItem : BaseItem, new()
{
    #region Private Static Fields

    // Default JSON node used when serialization returns null
    private static readonly JsonNode _jsonNodeEmpty = new JsonObject();

    #endregion

    #region Private Fields

    // Validator for base item properties like Id, PartitionKey, and TypeName
    private readonly IValidator<TItem> _baseItemValidator;

    // Allowed CRUD operations for this provider instance
    private readonly CommandOperations _commandOperations;

    // Optional custom validator for domain-specific validation rules
    private readonly IValidator<TItem>? _itemValidator;

    private readonly ILogger? _logger;

    // JSON serializer options for serializing events
    private readonly JsonSerializerOptions _optionsForSerializeEvent;

    // JSON serializer options for serializing items (may include encryption)
    private readonly JsonSerializerOptions _optionsForSerializeItem;

    // Function that serializes items for change tracking purposes
    private readonly Func<TItem, JsonNode?> _serializeChanges;

    // Type name identifier for items managed by this provider
    private readonly string _typeName;

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets the type name identifier for items managed by this provider.
    /// </summary>
    protected string TypeName => _typeName;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new data provider with validation and operation constraints.
    /// </summary>
    /// <param name="typeName">Type name identifier that must follow naming conventions.</param>
    /// <param name="itemValidator">Optional custom validator for domain-specific rules.</param>
    /// <param name="commandOperations">Allowed CRUD operations, defaults to Read-only.</param>
    /// <param name="eventPolicy">Optional event policy for change tracking.</param>
    /// <param name="blockCipherService">Optional service for field-level encryption.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentException">Thrown when typeName is invalid or reserved.</exception>
    protected DataProvider(
        string typeName,
        IValidator<TItem>? itemValidator,
        CommandOperations? commandOperations = null,
        EventPolicy? eventPolicy = null,
        IBlockCipherService? blockCipherService = null,
        ILogger? logger = null)
    {
        // Validate type name format (lowercase letters and hyphens only)
        if (TypeRulesRegex().IsMatch(typeName) is false)
        {
            throw new ArgumentException($"The typeName '{typeName}' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.", nameof(typeName));
        }

        // Ensure type name is not reserved for system use
        if (ReservedTypeNames.IsReserved(typeName))
        {
            throw new ArgumentException($"The typeName '{typeName}' is a reserved type name.", nameof(typeName));
        }

        _typeName = typeName;

        // Set up validators for base properties and optional custom validation
        _baseItemValidator = CreateBaseItemValidator(typeName);
        _itemValidator = itemValidator;

        // Configure allowed operations (default to read-only)
        _commandOperations = commandOperations ?? CommandOperations.Read;

        _logger = logger;

        // Configure JSON serialization options
        var defaultOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        // Options for serializing events
        _optionsForSerializeEvent = new JsonSerializerOptions(defaultOptions)
        {
        };

        // Options for serializing items (with optional encryption)
        _optionsForSerializeItem = new JsonSerializerOptions(defaultOptions)
        {
            TypeInfoResolver = blockCipherService is not null
                ? new CompositeJsonResolver(
                    [
                        new EncryptPropertyResolver(blockCipherService)
                    ])
                : null
        };

        var optionsForOnlyTrackAttributeChanges = new JsonSerializerOptions(defaultOptions)
        {
            TypeInfoResolver = blockCipherService is not null
                ? new CompositeJsonResolver(
                    [
                        new PropertyChangeResolver(allChanges: false),
                        new EncryptPropertyResolver(blockCipherService)
                    ])
                : new CompositeJsonResolver(
                    [
                        new PropertyChangeResolver(allChanges: false)
                    ])
        };

        var optionsForAllChanges = new JsonSerializerOptions(defaultOptions)
        {
            TypeInfoResolver = blockCipherService is not null
                ? new CompositeJsonResolver(
                    [
                        new PropertyChangeResolver(allChanges: true),
                        new EncryptPropertyResolver(blockCipherService)
                    ])
                : new CompositeJsonResolver(
                    [
                        new PropertyChangeResolver(allChanges: true)
                    ])
        };

        _serializeChanges = eventPolicy switch
        {
            EventPolicy.NoChanges => item => _jsonNodeEmpty,
            EventPolicy.OnlyTrackAttributeChanges => item => JsonSerializer.SerializeToNode(
                value: item,
                options: optionsForOnlyTrackAttributeChanges) ?? _jsonNodeEmpty,
            null or EventPolicy.AllChanges => item => JsonSerializer.SerializeToNode(
                value: item,
                options: optionsForAllChanges) ?? _jsonNodeEmpty,
            _ => item => null
        };
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public IBatchCommand<TItem> Batch()
    {
        return new BatchCommand<TItem>(SaveBatchAsync);
    }

    /// <inheritdoc />
    public ISaveCommand<TItem> Create(
        string id,
        string partitionKey)
    {
        if (_commandOperations.HasFlag(CommandOperations.Create) is false)
        {
            throw new NotSupportedException("The requested Create operation is not supported.");
        }

        var dateTimeOffsetUtcNow = DateTimeOffset.UtcNow;

        // Initialize new item with required properties
        var item = new TItem
        {
            Id = id,
            PartitionKey = partitionKey,
            TypeName = _typeName,
            Version = 1,
            CreatedDateTimeOffset = dateTimeOffsetUtcNow,
            UpdatedDateTimeOffset = dateTimeOffsetUtcNow,
        };

        return SaveCommand<TItem>.Create(
            item: item,
            saveAction: SaveAction.CREATED,
            serializeChanges: _serializeChanges,
            validateAsyncDelegate: ValidateAsync,
            saveAsyncDelegate: SaveItemAsync,
            logger: _logger);
    }

    /// <inheritdoc />
    public async Task<ISaveCommand<TItem>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        if (_commandOperations.HasFlag(CommandOperations.Delete) is false)
        {
            throw new NotSupportedException("The requested Delete operation is not supported.");
        }

        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        // Return null if item doesn't exist or is already deleted
        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        return CreateDeleteCommand(item);
    }

    /// <inheritdoc />
    public IQueryCommand<TItem> Query()
    {
        var queryable = CreateQueryable();

        return new QueryCommand<TItem>(
            queryable: queryable,
            queryAsyncDelegate: q => ExecuteQueryableAsync(q));
    }

    /// <inheritdoc />
    public async Task<IReadResult<TItem>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        // Return null if item doesn't exist or is deleted
        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        return ReadResult<TItem>.Create(
            item: item,
            logger: _logger);
    }

    /// <inheritdoc />
    public async Task<ISaveCommand<TItem>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        if (_commandOperations.HasFlag(CommandOperations.Update) is false)
        {
            throw new NotSupportedException("The requested Update operation is not supported.");
        }

        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        // Return null if item doesn't exist or is already deleted
        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        return CreateUpdateCommand(item);
    }

    #endregion

    #region Protected Abstract Methods

    /// <summary>
    /// Creates a queryable data source for LINQ operations.
    /// </summary>
    /// <returns>IQueryable instance for the item type.</returns>
    protected abstract IQueryable<TItem> CreateQueryable();

    /// <summary>
    /// Executes a query against the data store and returns results asynchronously.
    /// </summary>
    /// <param name="queryable">The query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the query operation.</param>
    /// <returns>Asynchronous enumerable of matching items.</returns>
    /// <remarks>
    /// Implementations should decorate the <paramref name="cancellationToken"/> parameter with
    /// <see cref="EnumeratorCancellationAttribute"/> to receive the cancellation token from the
    /// async enumerator when consumers use <c>.WithCancellation(token)</c>.
    /// </remarks>
    protected abstract IAsyncEnumerable<IQueryResult<TItem>> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single item from the data store by its identifiers.
    /// </summary>
    /// <param name="id">Unique identifier of the item.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The item if found, or null if not found.</returns>
    protected abstract Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple items to the data store in a single atomic operation.
    /// </summary>
    /// <param name="requests">Array of save requests to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of save results indicating success or failure for each item.</returns>
    protected abstract Task<SaveResult<TItem>[]> SaveBatchAsync(
        SaveRequest<TItem>[] requests,
        CancellationToken cancellationToken = default);

    #endregion

    #region Protected Methods

    /// <summary>
    /// Converts an item to a query result with command operations.
    /// </summary>
    /// <param name="item">The item to convert.</param>
    /// <returns>A query result wrapping the item with delete and update command capabilities.</returns>
    /// <remarks>
    /// This method is used by derived classes in their query execution to convert each retrieved
    /// item into an <see cref="IQueryResult{TItem}"/> that provides command operations.
    /// </remarks>
    protected IQueryResult<TItem> ConvertToQueryResult(
        TItem item)
    {
        return QueryResult<TItem>.Create(
            item: item,
            createDeleteCommand: CreateDeleteCommand,
            createUpdateCommand: CreateUpdateCommand,
            logger: _logger);
    }

    /// <summary>
    /// Deserializes a JSON string into an item using the configured serialization options.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <returns>The deserialized item or null.</returns>
    protected TItem? DeserializeItem(
        string json)
    {
        return JsonSerializer.Deserialize<TItem>(
            json: json,
            options: _optionsForSerializeItem);
    }

    /// <summary>
    /// Deserializes a JSON stream into an item using the configured serialization options.
    /// </summary>
    /// <param name="utf8Json">Stream containing JSON data.</param>
    /// <returns>The deserialized item or null.</returns>
    protected TItem? DeserializeItem(
        Stream utf8Json)
    {
        return JsonSerializer.Deserialize<TItem>(
            utf8Json: utf8Json,
            options: _optionsForSerializeItem);
    }

    /// <summary>
    /// Deserializes a JSON node into an item using the configured serialization options.
    /// </summary>
    /// <param name="jsonNode">JSON node to deserialize.</param>
    /// <returns>The deserialized item or null.</returns>
    protected TItem? DeserializeItem(
        JsonNode jsonNode)
    {
        return jsonNode.Deserialize<TItem>(
            options: _optionsForSerializeItem);
    }

    /// <summary>
    /// Serializes an item event to a JSON string.
    /// </summary>
    /// <param name="itemEvent">Event to serialize.</param>
    /// <returns>JSON string representation of the event.</returns>
    protected string SerializeEvent<TItemEvent>(
        TItemEvent itemEvent)
        where TItemEvent : ItemEvent
    {
        return JsonSerializer.Serialize(
            value: itemEvent,
            options: _optionsForSerializeEvent);
    }

    /// <summary>
    /// Serializes an item event to a JSON stream.
    /// </summary>
    /// <param name="utf8Json">Stream to write JSON data to.</param>
    /// <param name="itemEvent">Event to serialize.</param>
    protected void SerializeEvent<TItemEvent>(
        Stream utf8Json,
        TItemEvent itemEvent)
        where TItemEvent : ItemEvent
    {
        JsonSerializer.Serialize(
            utf8Json: utf8Json,
            value: itemEvent,
            options: _optionsForSerializeEvent);

        utf8Json.Position = 0;
    }

    /// <summary>
    /// Serializes an item event to a JSON node.
    /// </summary>
    /// <param name="itemEvent">Event to serialize.</param>
    /// <returns>JSON node representation of the event.</returns>
    protected JsonNode SerializeEventToNode<TItemEvent>(
        TItemEvent itemEvent)
        where TItemEvent : ItemEvent
    {
        return JsonSerializer.SerializeToNode(
            value: itemEvent,
            options: _optionsForSerializeEvent) ?? new JsonObject();
    }

    /// <summary>
    /// Serializes an item to a JSON string using the configured serialization options.
    /// </summary>
    /// <param name="item">Item to serialize.</param>
    /// <returns>JSON string representation of the item.</returns>
    protected string SerializeItem(
        TItem item)
    {
        return JsonSerializer.Serialize(
            value: item,
            options: _optionsForSerializeItem);
    }

    /// <summary>
    /// Serializes an item to a JSON stream using the configured serialization options.
    /// </summary>
    /// <param name="utf8Json">Stream to write JSON data to.</param>
    /// <param name="item">Item to serialize.</param>
    protected void SerializeItem(
        Stream utf8Json,
        TItem item)
    {
        JsonSerializer.Serialize(
            utf8Json: utf8Json,
            value: item,
            options: _optionsForSerializeItem);

        utf8Json.Position = 0;
    }

    /// <summary>
    /// Serializes an item to a JSON node using the configured serialization options.
    /// </summary>
    /// <param name="item">Item to serialize.</param>
    /// <returns>JSON node representation of the item.</returns>
    protected JsonNode SerializeItemToNode(
        TItem item)
    {
        return JsonSerializer.SerializeToNode(
            value: item,
            options: _optionsForSerializeItem) ?? new JsonObject();
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a validator for base item properties that all items must satisfy.
    /// </summary>
    /// <param name="typeName">Expected type name for validation.</param>
    /// <returns>Validator that checks required base properties.</returns>
    private static IValidator<TItem> CreateBaseItemValidator(
        string typeName)
    {
        var baseItemValidator = new InlineValidator<TItem>();

        baseItemValidator.RuleFor(k => k.Id)
            .NotEmpty()
            .WithMessage("Id is null or empty.");

        baseItemValidator.RuleFor(k => k.PartitionKey)
            .NotEmpty()
            .WithMessage("PartitionKey is null or empty.");

        baseItemValidator.RuleFor(k => k.TypeName)
            .Must(k => k == typeName)
            .WithMessage($"TypeName is not '{typeName}'.");

        baseItemValidator.RuleFor(k => k.CreatedDateTimeOffset)
            .NotDefault()
            .WithMessage("CreatedDateTimeOffset is not valid.");

        baseItemValidator.RuleFor(k => k.UpdatedDateTimeOffset)
            .NotDefault()
            .WithMessage("UpdatedDateTimeOffset is not valid.");

        return baseItemValidator;
    }

    /// <summary>
    /// Provides a regex pattern for validating type name format.
    /// </summary>
    /// <returns>Regex that matches valid type names.</returns>
    [GeneratedRegex(@"^[a-z]+[a-z-]*[a-z]+$")]
    private static partial Regex TypeRulesRegex();

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a save command for soft-deleting an item by setting deletion flags.
    /// </summary>
    /// <param name="item">Item to mark as deleted.</param>
    /// <returns>Save command configured for deletion.</returns>
    private ISaveCommand<TItem> CreateDeleteCommand(
        TItem item)
    {
        // Increment version and set deletion properties
        item.Version = item.Version + 1;
        item.DeletedDateTimeOffset = DateTimeOffset.UtcNow;
        item.IsDeleted = true;

        return SaveCommand<TItem>.Create(
            item: item,
            saveAction: SaveAction.DELETED,
            serializeChanges: _serializeChanges,
            validateAsyncDelegate: ValidateAsync,
            saveAsyncDelegate: SaveItemAsync,
            logger: _logger);
    }

    /// <summary>
    /// Creates a save command for updating an item with a new version and timestamp.
    /// </summary>
    /// <param name="item">Item to prepare for update.</param>
    /// <returns>Save command configured for update.</returns>
    private ISaveCommand<TItem> CreateUpdateCommand(
        TItem item)
    {
        // Increment version and update timestamp
        item.Version++;
        item.UpdatedDateTimeOffset = DateTimeOffset.UtcNow;

        return SaveCommand<TItem>.Create(
            item: item,
            saveAction: SaveAction.UPDATED,
            serializeChanges: _serializeChanges,
            validateAsyncDelegate: ValidateAsync,
            saveAsyncDelegate: SaveItemAsync,
            logger: _logger);
    }

    /// <summary>
    /// Saves a single item by wrapping it in a batch operation and extracting the result.
    /// </summary>
    /// <param name="request">Save request for the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The saved item.</returns>
    /// <exception cref="CommandException">Thrown when the save operation fails.</exception>
    private async Task<TItem> SaveItemAsync(
        SaveRequest<TItem> request,
        CancellationToken cancellationToken = default)
    {
        // Wrap single item in array for batch processing
        SaveRequest<TItem>[] requests = [ request ];

        var results = await SaveBatchAsync(
            requests: requests,
            cancellationToken: cancellationToken);

        var result = results[0];

        // Throw exception if save failed
        if (result.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new CommandException(result.HttpStatusCode);
        }

        return result.Item!;
    }

    /// <summary>
    /// Validates an item using both base property validation and custom validation rules.
    /// </summary>
    /// <param name="item">Item to validate.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Validation result containing any errors found.</returns>
    private async Task<ValidationResult> ValidateAsync(
        TItem item,
        CancellationToken cancellationToken)
    {
        // Combine base validation with custom validation
        var compositeValidator = new CompositeValidator<TItem>(_baseItemValidator, _itemValidator);

        return await compositeValidator.ValidateAsync(item, cancellationToken);
    }

    #endregion
}
