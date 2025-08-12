using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides data access operations for a specific entity type with validation and command support.
/// </summary>
/// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
/// <remarks>
/// Standardized interface for CRUD operations across different data store implementations.
/// </remarks>
public interface IDataProvider<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Creates a batch command for executing multiple operations atomically.
    /// </summary>
    /// <returns>A batch command instance for grouping multiple operations.</returns>
    IBatchCommand<TInterface> Batch();

    /// <summary>
    /// Creates a new item with the specified identifier and partition key.
    /// </summary>
    /// <param name="id">Unique identifier for the new item.</param>
    /// <param name="partitionKey">Partition key for data distribution.</param>
    /// <returns>A save command for configuring and persisting the new item.</returns>
    /// <exception cref="NotSupportedException">Thrown when Create operations are not permitted for this provider.</exception>
    ISaveCommand<TInterface> Create(
        string id,
        string partitionKey);

    /// <summary>
    /// Marks an item for deletion using soft-delete semantics.
    /// </summary>
    /// <param name="id">Unique identifier of the item to delete.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task containing a save command for the deletion operation, or null if the item is not found or already deleted.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when Delete operations are not permitted for this provider.</exception>
    Task<ISaveCommand<TInterface>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an item by its identifier and partition key.
    /// </summary>
    /// <param name="id">Unique identifier of the item.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task containing a read-only result with the item data, or null if not found or deleted.
    /// </returns>
    Task<IReadResult<TInterface>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a LINQ-enabled query command for complex data retrieval.
    /// </summary>
    /// <returns>A query command supporting LINQ operations with deferred execution.</returns>
    IQueryCommand<TInterface> Query();

    /// <summary>
    /// Prepares an existing item for modification.
    /// </summary>
    /// <param name="id">Unique identifier of the item to update.</param>
    /// <param name="partitionKey">Partition key of the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A task containing a save command with the modifiable item, or null if not found or deleted.
    /// </returns>
    /// <exception cref="NotSupportedException">Thrown when Update operations are not permitted for this provider.</exception>
    Task<ISaveCommand<TInterface>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstract base implementation providing common data provider functionality.
/// </summary>
/// <typeparam name="TInterface">The interface type defining the entity contract.</typeparam>
/// <typeparam name="TItem">The concrete entity implementation type.</typeparam>
public abstract partial class DataProvider<TInterface, TItem>
    : IDataProvider<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Protected Fields

    /// <summary>
    /// The type name identifier stored in all items managed by this provider.
    /// </summary>
    protected readonly string _typeName;

    #endregion

    #region Private Fields

    /// <summary>
    /// Validator for item base properties like Id, PartitionKey, and timestamps.
    /// </summary>
    private readonly IValidator<TItem> _baseItemValidator;

    /// <summary>
    /// Bitwise flags defining which CRUD operations are permitted for this provider.
    /// </summary>
    private readonly CommandOperations _commandOperations;

    /// <summary>
    /// Converts LINQ expressions from interface type to concrete implementation type.
    /// </summary>
    private readonly ExpressionConverter<TInterface, TItem> _expressionConverter;

    /// <summary>
    /// Optional domain-specific validator for custom business rules validation.
    /// </summary>
    private readonly IValidator<TItem>? _itemValidator;

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets the type name identifier used for items managed by this provider.
    /// </summary>
    protected string TypeName => _typeName;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new data provider instance with validation and operation constraints.
    /// </summary>
    /// <param name="typeName">Type name identifier that must follow naming conventions (lowercase letters and hyphens).</param>
    /// <param name="validator">Optional custom validator for domain-specific validation rules.</param>
    /// <param name="commandOperations">Permitted CRUD operations. Defaults to Read-only if not specified.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when typeName doesn't match naming rules or conflicts with reserved system names.
    /// </exception>
    protected DataProvider(
        string typeName,
        IValidator<TItem>? validator,
        CommandOperations? commandOperations = null)
    {
        // Validate the type name against the naming rules (lowercase letters and hyphens).
        if (TypeRulesRegex().IsMatch(typeName) is false)
        {
            throw new ArgumentException($"The typeName '{typeName}' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.", nameof(typeName));
        }

        // Ensure the type name doesn't conflict with any system-reserved names.
        if (ReservedTypeNames.IsReserved(typeName))
        {
            throw new ArgumentException($"The typeName '{typeName}' is a reserved type name.", nameof(typeName));
        }

        // Store the validated type name for use in item creation and queries.
        _typeName = typeName;

        // Set up validators for both base properties and custom properties.
        _baseItemValidator = CreateBaseItemValidator(typeName);
        _itemValidator = validator;

        // Create the expression converter for LINQ operations.
        _expressionConverter = new();

        // Set the allowed operations for this provider, defaulting to Read only if not specified.
        _commandOperations = commandOperations ?? CommandOperations.Read;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public IBatchCommand<TInterface> Batch()
    {
        return new BatchCommand<TInterface, TItem>(SaveBatchAsync);
    }

    /// <inheritdoc />
    public ISaveCommand<TInterface> Create(
        string id,
        string partitionKey)
    {
        if (_commandOperations.HasFlag(CommandOperations.Create) is false)
        {
            throw new NotSupportedException("The requested Create operation is not supported.");
        }

        var dateTimeOffsetUtcNow = DateTimeOffset.UtcNow;

        var item = new TItem
        {
            Id = id,
            PartitionKey = partitionKey,
            TypeName = _typeName,
            Version = 1,
            CreatedDateTimeOffset = dateTimeOffsetUtcNow,
            UpdatedDateTimeOffset = dateTimeOffsetUtcNow,
        };

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: false,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.CREATED,
            saveAsyncDelegate: SaveItemAsync);
    }

    /// <inheritdoc />
    public async Task<ISaveCommand<TInterface>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        if (_commandOperations.HasFlag(CommandOperations.Delete) is false)
        {
            throw new NotSupportedException("The requested Delete operation is not supported.");
        }

        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        return CreateDeleteCommand(item);
    }

    /// <inheritdoc />
    public IQueryCommand<TInterface> Query()
    {
        var queryable = CreateQueryable();

        Func<TItem, IQueryResult<TInterface>> convertToQueryResult = item => {
            return QueryResult<TInterface, TItem>.Create(
                item: item,
                validateAsyncDelegate: ValidateAsync,
                createDeleteCommand: CreateDeleteCommand,
                createUpdateCommand: CreateUpdateCommand);
        };

        return new QueryCommand<TInterface, TItem>(
            expressionConverter: _expressionConverter,
            queryable: queryable,
            queryAsyncDelegate: ExecuteQueryableAsync,
            convertToQueryResult: convertToQueryResult);
    }

    /// <inheritdoc />
    public async Task<IReadResult<TInterface>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        return ReadResult<TInterface, TItem>.Create(
            item: item,
            validateAsyncDelegate: ValidateAsync);
    }

    /// <inheritdoc />
    public async Task<ISaveCommand<TInterface>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        if (_commandOperations.HasFlag(CommandOperations.Update) is false)
        {
            throw new NotSupportedException("The requested Update operation is not supported.");
        }

        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

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
    /// <returns>An IQueryable instance for the concrete item type.</returns>
    protected abstract IQueryable<TItem> CreateQueryable();

    /// <summary>
    /// Executes a LINQ query against the data store asynchronously.
    /// </summary>
    /// <param name="queryable">The LINQ query expression to execute.</param>
    /// <param name="cancellationToken">Token to cancel the query execution.</param>
    /// <returns>An async enumerable of items matching the query criteria.</returns>
    protected abstract IAsyncEnumerable<TItem> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single item from the data store by its identifiers.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <param name="partitionKey">The partition key for data distribution.</param>
    /// <param name="cancellationToken">Token to cancel the read operation.</param>
    /// <returns>The item if found, or null if it doesn't exist in the data store.</returns>
    protected abstract Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists multiple items to the data store in a single atomic operation.
    /// </summary>
    /// <param name="requests">Array of save requests containing items and their save actions.</param>
    /// <param name="cancellationToken">Token to cancel the batch save operation.</param>
    /// <returns>Array of save results indicating success/failure status for each item.</returns>
    protected abstract Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default);

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a FluentValidation validator for required item base properties.
    /// </summary>
    /// <param name="typeName">The expected type name for validation.</param>
    /// <returns>A validator that checks Id, PartitionKey, TypeName, and timestamp properties.</returns>
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
    /// Generates a regular expression pattern for validating type name format.
    /// </summary>
    /// <returns>A regex that matches valid type names (lowercase letters and hyphens, starting and ending with letters).</returns>
    [GeneratedRegex(@"^[a-z]+[a-z-]*[a-z]+$")]
    private static partial Regex TypeRulesRegex();

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a save command for soft-deleting an item by setting deletion flags and timestamp.
    /// </summary>
    /// <param name="item">The item to mark as deleted.</param>
    /// <returns>A read-only save command that will perform the soft deletion when executed.</returns>
    private ISaveCommand<TInterface> CreateDeleteCommand(
        TItem item)
    {
        item.Version = item.Version + 1;

        item.DeletedDateTimeOffset = DateTimeOffset.UtcNow;
        item.IsDeleted = true;

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: true,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.DELETED,
            saveAsyncDelegate: SaveItemAsync);
    }

    /// <summary>
    /// Creates a save command for updating an item with a new timestamp.
    /// </summary>
    /// <param name="item">The item to prepare for update.</param>
    /// <returns>A modifiable save command for the item update operation.</returns>
    private ISaveCommand<TInterface> CreateUpdateCommand(
        TItem item)
    {
        item.Version = item.Version + 1;

        item.UpdatedDateTimeOffset = DateTimeOffset.UtcNow;

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: false,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.UPDATED,
            saveAsyncDelegate: SaveItemAsync);
    }

    /// <summary>
    /// Persists a single item by wrapping it in a batch operation.
    /// </summary>
    /// <param name="request">The save request containing the item and operation details.</param>
    /// <param name="cancellationToken">Token to cancel the save operation.</param>
    /// <returns>The successfully saved item.</returns>
    /// <exception cref="CommandException">Thrown when the save operation fails with a non-success HTTP status.</exception>
    private async Task<TItem> SaveItemAsync(
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken = default)
    {
        SaveRequest<TInterface, TItem>[] requests = [ request ];

        var results = await SaveBatchAsync(
            requests: requests,
            cancellationToken: cancellationToken);

        var result = results[0];

        if (result.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new CommandException(result.HttpStatusCode);
        }

        return result.Item!;
    }

    /// <summary>
    /// Validates an item using both base item validator and item validator
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="cancellationToken">Token to cancel the validation operation.</param>
    /// <returns>A validation result containing any errors or warnings found.</returns>
    private async Task<ValidationResult> ValidateAsync(
        TItem item,
        CancellationToken cancellationToken)
    {
        var compositeValidator = new CompositeValidator<TItem>(_baseItemValidator, _itemValidator);

        return await compositeValidator.ValidateAsync(item, cancellationToken);
    }

    #endregion
}
