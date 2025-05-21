using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Command pattern interface for data store operations.
/// </summary>
/// <typeparam name="TInterface">Item interface type.</typeparam>
/// <remarks>
/// Standardized interface for different data stores.
/// </remarks>
public interface ICommandProvider<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Creates a batch command.
    /// </summary>
    /// <returns>Batch command.</returns>
    /// <remarks>
    /// Executes multiple operations atomically.
    /// </remarks>
    IBatchCommand<TInterface> Batch();

    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <returns>Command for configuring and persisting the new item.</returns>
    /// <remarks>
    /// Initializes an item but doesn't persist it until the command is executed.
    /// </remarks>
    ISaveCommand<TInterface> Create(
        string id,
        string partitionKey);

    /// <summary>
    /// Marks an item for deletion (soft-delete).
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Command for the deletion operation, or null if item not found.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// When Delete operation isn't supported.
    /// </exception>
    Task<ISaveCommand<TInterface>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an item.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Read-only result containing the item, or null if not found.
    /// </returns>
    Task<IReadResult<TInterface>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a LINQ query command.
    /// </summary>
    /// <returns>Query command with LINQ capabilities.</returns>
    /// <remarks>
    /// Enables LINQ querying with deferred execution.
    /// </remarks>
    IQueryCommand<TInterface> Query();

    /// <summary>
    /// Prepares an existing item for modification.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="partitionKey">Partition key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Command with modifiable item, or null if not found.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// When Update operation isn't supported.
    /// </exception>
    Task<ISaveCommand<TInterface>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Base implementation for command providers.
/// </summary>
/// <typeparam name="TInterface">Item interface type.</typeparam>
/// <typeparam name="TItem">Concrete implementation type.</typeparam>
/// <remarks>
/// Implements command pattern for data access.
/// </remarks>
public abstract partial class CommandProvider<TInterface, TItem>
    : ICommandProvider<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Protected Fields

    /// <summary>
    /// Type name stored in items.
    /// </summary>
    protected readonly string _typeName;

    #endregion

    #region Private Fields

    /// <summary>
    /// Validator for base item properties.
    /// </summary>
    private readonly IValidator<TItem> _baseItemValidator;

    /// <summary>
    /// Allowed operations.
    /// </summary>
    private readonly CommandOperations _commandOperations;

    /// <summary>
    /// Converts interface expressions to implementation expressions.
    /// </summary>
    private readonly ExpressionConverter<TInterface, TItem> _expressionConverter;

    /// <summary>
    /// Domain-specific validator.
    /// </summary>
    private readonly IValidator<TItem>? _itemValidator;

    #endregion

    #region Protected Properties

    /// <summary>
    /// Type name for items.
    /// </summary>
    protected string TypeName => _typeName;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="typeName">The type name of the item.</param>
    /// <param name="validator">The optional validator.</param>
    /// <param name="commandOperations">The operations allowed (default: Read only).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="typeName"/> is invalid.
    /// </exception>
    /// <remarks>
    /// The constructor sets up validators and verifies the type name.
    /// </remarks>
    protected CommandProvider(
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

        // Set up validators for both base properties and custom item properties.
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
            executeQueryable: ExecuteQueryable,
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
    /// Creates an <see cref="IQueryable{TItem}"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable{TItem}"/>.
    /// </returns>
    protected abstract IQueryable<TItem> CreateQueryable();

    /// <summary>
    /// Executes a LINQ query.
    /// </summary>
    /// <param name="queryable">The LINQ query to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An enumerable of items matching the query.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    protected abstract IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an item.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// The item if found, or <see langword="null"/> if the item does not exist.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    protected abstract Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a batch of items.
    /// </summary>
    /// <param name="requests">An array of save requests.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An array of <see cref="SaveResult{TInterface, TItem}"/> objects.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    protected abstract Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default);

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a validator for the base properties of all items.
    /// </summary>
    /// <param name="typeName">The expected type name.</param>
    /// <returns>An <see cref="IValidator{TItem}"/>.</returns>
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
    /// Creates a regular expression that enforces the naming rules for type names.
    /// </summary>
    /// <returns>A <see cref="Regex"/> that matches valid type names.</returns>
    [GeneratedRegex(@"^[a-z]+[a-z-]*[a-z]+$")]
    private static partial Regex TypeRulesRegex();

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a command to mark an item as deleted.
    /// </summary>
    /// <param name="item">The item to mark as deleted.</param>
    /// <returns>
    /// An <see cref="ISaveCommand{TInterface}"/> that can be executed to perform the deletion.
    /// </returns>
    private ISaveCommand<TInterface> CreateDeleteCommand(
        TItem item)
    {
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
    /// Creates a command to update an item.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <returns>
    /// An <see cref="ISaveCommand{TInterface}"/> that can be executed to perform the update.
    /// </returns>
    private ISaveCommand<TInterface> CreateUpdateCommand(
        TItem item)
    {
        item.UpdatedDateTimeOffset = DateTimeOffset.UtcNow;

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: false,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.UPDATED,
            saveAsyncDelegate: SaveItemAsync);
    }

    /// <summary>
    /// Saves a single item.
    /// </summary>
    /// <param name="request">The save request.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// The saved item.
    /// </returns>
    /// <exception cref="CommandException">
    /// Thrown when the save operation fails.
    /// </exception>
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
    /// Validates an item.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// The validation result.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled.
    /// </exception>
    private async Task<ValidationResult> ValidateAsync(
        TItem item,
        CancellationToken cancellationToken)
    {
        var compositeValidator = new CompositeValidator<TItem>(_baseItemValidator, _itemValidator);

        return await compositeValidator.ValidateAsync(item, cancellationToken);
    }

    #endregion
}
