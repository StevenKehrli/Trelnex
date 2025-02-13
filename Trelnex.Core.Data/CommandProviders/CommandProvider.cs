using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// The interface to expose the commands against the backing data store.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
public interface ICommandProvider<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Returns an <see cref="ISaveCommand{TInterface}"/> which wraps the item.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item that was created.</returns>
    ISaveCommand<TInterface> Create(
        string id,
        string partitionKey);

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation
    /// and returns an <see cref="ISaveCommand{TInterface}"/> which wraps the item.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item that was read.</returns>
    Task<ISaveCommand<TInterface>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="IReadResult{TInterface}"/> with the item that was read.</returns>
    Task<IReadResult<TInterface>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation
    /// and returns an <see cref="ISaveCommand{TInterface}"/> which wraps the item.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item that was read.</returns>
    Task<ISaveCommand<TInterface>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a batch of commands for the backing data store.
    /// </summary>
    /// <returns>An <see cref="IBatchCommand{TInterface}"/> to batch the commands for the backing data store.</returns>
    IBatchCommand<TInterface> Batch();

    /// <summary>
    /// Creates a LINQ query for items from the backing data store.
    /// </summary>
    /// <returns>The <see cref="IQueryCommand{TInterface}"/>.</returns>
    IQueryCommand<TInterface> Query();
}

public abstract partial class CommandProvider<TInterface, TItem>
    : ICommandProvider<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    /// <summary>
    /// The type name of the item.
    /// </summary>
    protected readonly string _typeName;

    /// <summary>
    /// The fluent validator for the base item.
    /// </summary>
    private readonly AbstractValidator<TItem> _baseItemValidator;

    /// <summary>
    /// The fluent validator for the item.
    /// </summary>
    private readonly AbstractValidator<TItem>? _itemValidator;

    /// <summary>
    /// The <see cref="ExpressionConverter{TInterface,TItem}"/> to convert an expression using a TInterface to an expression using a TItem.
    /// </summary>
    private readonly ExpressionConverter<TInterface, TItem> _expressionConverter;

    /// <summary>
    /// The command operations allowed by this provider.
    /// </summary>
    private readonly CommandOperations _commandOperations;

    /// <summary>
    /// The delegate to validate and save the item.
    /// </summary>
    private readonly SaveAsyncDelegate<TInterface, TItem> _saveAsyncDelegate;

    /// <summary>
    /// The delegate to validate and save a batch of items.
    /// </summary>
    private readonly SaveBatchAsyncDelegate<TInterface, TItem> _saveBatchAsyncDelegate;

    /// <summary>
    /// The type name of the item - used for <see cref="BaseItem.TypeName"/>.
    /// </summary>
    protected string TypeName => _typeName;

    /// <summary>
    ///
    /// </summary>
    /// <param name="typeName">The type name of the item.</param>
    /// <param name="validator">The fluent validator for the item.</param>
    /// <param name="commandOperations">The command operations allowed by this provider.</param>
    protected CommandProvider(
        string typeName,
        AbstractValidator<TItem>? validator,
        CommandOperations? commandOperations = null)
    {
        // validate the type folloows the naming rules
        if (TypeRulesRegex().IsMatch(typeName) is false)
        {
            throw new ArgumentException($"The typeName '{typeName}' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.", nameof(typeName));
        }

        // validate not a reserved type
        if (ReservedTypeNames.IsReserved(typeName))
        {
            throw new ArgumentException($"The typeName '{typeName}' is a reserved type name.", nameof(typeName));
        }

        // set the inputs
        _typeName = typeName;

        // set the validators
        _baseItemValidator = CreateBaseItemValidator(typeName);
        _itemValidator = validator;

        // create the expression converter
        _expressionConverter = new();

        // the command operations allowed by this provider
        _commandOperations = commandOperations ?? CommandOperations.Update;

        _saveAsyncDelegate = (request, cancellationToken) =>
        {
            if (string.Equals(request.Event.PartitionKey, request.Item.PartitionKey) is false)
            {
                throw new CommandException(
                    httpStatusCode: HttpStatusCode.BadRequest,
                    message: "The PartitionKey for the itemEvent does not match the one specified for the item.");
            }

            return SaveItemAsync(request, cancellationToken);
        };

        _saveBatchAsyncDelegate = (partitionKey, requests, cancellationToken) =>
        {
            // check the partition keys
            var partitionKeyCheck = (SaveRequest<TInterface, TItem> request) =>
                string.Equals(request.Item.PartitionKey, partitionKey) &&
                string.Equals(request.Event.PartitionKey, request.Item.PartitionKey);

            // if any of the items do not have the correct partition key, return a bad request
            if (requests.Any(request => partitionKeyCheck(request)) is false)
            {
                // allocate the results
                var saveResults = new SaveResult<TInterface, TItem>[requests.Length];

                for (var index = 0; index < requests.Length; index++)
                {
                    var request = requests[index];

                    // if the partition key does match, return a failed dependency; otherwise, return a bad request
                    var httpStatusCode = partitionKeyCheck(request)
                        ? HttpStatusCode.FailedDependency
                        : HttpStatusCode.BadRequest;

                    saveResults[index] = new SaveResult<TInterface, TItem>(httpStatusCode, null);
                }

                return Task.FromResult(saveResults);
            }

            return SaveBatchAsync(partitionKey, requests, cancellationToken);
        };
    }

    /// <summary>
    /// Returns an <see cref="ISaveCommand{TInterface}"/> which wraps the item.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item that was created.</returns>
    public ISaveCommand<TInterface> Create(
        string id,
        string partitionKey)
    {
        var dateTimeUtcNow = DateTime.UtcNow;

        var item = new TItem
        {
            Id = id,
            PartitionKey = partitionKey,

            TypeName = _typeName,

            CreatedDate = dateTimeUtcNow,
            UpdatedDate = dateTimeUtcNow,
        };

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: false,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.CREATED,
            saveAsyncDelegate: _saveAsyncDelegate);
    }

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation
    /// and returns an <see cref="ISaveCommand{TInterface}"/> which wraps the item.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item that was read.</returns>
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

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The item that was read.</returns>
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

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation
    /// and returns an <see cref="ISaveCommand{TInterface}"/> which wraps the item.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item that was read.</returns>
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

    /// <summary>
    /// Creates a batch of commands for the backing data store.
    /// </summary>
    /// <returns>An <see cref="IBatchCommand{TInterface}"/> to batch the commands for the backing data store.</returns>
    public IBatchCommand<TInterface> Batch()
    {
        return new BatchCommand<TInterface, TItem>(_saveBatchAsyncDelegate);
    }

    /// <summary>
    /// Creates a LINQ query for items from the backing data store.
    /// </summary>
    /// <returns>The <see cref="IQueryCommand{TInterface}"/>.</returns>
    public IQueryCommand<TInterface> Query()
    {
        // create the queryable
        var queryable = CreateQueryable();

        // create the convertToQueryResult delegate
        Func<TItem, IQueryResult<TInterface>> convertToQueryResult = item => {
            return QueryResult<TInterface, TItem>.Create(
                item: item,
                validateAsyncDelegate: ValidateAsync,
                createDeleteCommand: CreateDeleteCommand,
                createUpdateCommand: CreateUpdateCommand);
        };

        // create the query command
        return new QueryCommand<TInterface, TItem>(
            expressionConverter: _expressionConverter,
            queryable: queryable,
            executeQueryable: ExecuteQueryable,
            convertToQueryResult: convertToQueryResult);
    }

    /// <summary>
    /// Reads a item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The id of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The item that was read.</returns>
    protected abstract Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a batch of items in the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="partitionKey">The partition key of the batch.</param>
    /// <param name="requests">The batch of save requests with item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The results of the batch operation.</returns>
    protected abstract Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        string partitionKey,
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create the <see cref="IQueryable{TItem}"/> to query the items.
    /// </summary>
    /// <returns></returns>
    protected abstract IQueryable<TItem> CreateQueryable();

    /// <summary>
    /// Execute the query specified by the <see cref="IQueryable{TItem}"/> and return the results as an enumerable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The <see cref="IEnumerable{TInterface}"/>.</returns>
    protected abstract IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an <see cref="ISaveCommand{TInterface}"/> to delete the item.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item to delete.</returns>
    private ISaveCommand<TInterface> CreateDeleteCommand(
        TItem item)
    {
        item.DeletedDate = DateTime.UtcNow;
        item.IsDeleted = true;

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: true,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.DELETED,
            saveAsyncDelegate: _saveAsyncDelegate);
    }

    /// <summary>
    /// Create an <see cref="ISaveCommand{TInterface}"/> to update the item.
    /// </summary>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> with the item to delete.</returns>
    private ISaveCommand<TInterface> CreateUpdateCommand(
        TItem item)
    {
        item.UpdatedDate = DateTime.UtcNow;

        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: false,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.UPDATED,
            saveAsyncDelegate: _saveAsyncDelegate);
    }

    /// <summary>
    /// Saves a item in the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="request">The save request with item and event to save.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> representing request cancellation.</param>
    /// <returns>The item that was saved.</returns>
    private async Task<TItem> SaveItemAsync(
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken = default)
    {
        // batch the item and event
        SaveRequest<TInterface, TItem>[] requests = [ request ];

        var results = await SaveBatchAsync(
            partitionKey: request.Item.PartitionKey,
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
    /// Validate the item using the base item validator and the item validation.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The fluent <see cref="ValidationResult"/>.</returns>
    private async Task<ValidationResult> ValidateAsync(
        TItem item,
        CancellationToken cancellationToken)
    {
        // create a composite of the base item validator and the item validator
        var compositeValidator = new CompositeValidator<TItem>(_baseItemValidator, _itemValidator);

        // validate the item
        return await compositeValidator.ValidateAsync(item, cancellationToken);
    }

    [GeneratedRegex(@"^[a-z]+[a-z-]*[a-z]+$")]
    private static partial Regex TypeRulesRegex();

    /// <summary>
    /// Create an instance of <see cref="AbstractValidator{TItem}"/> to validate the base item.
    /// </summary>
    /// <param name="typeName">The type name of the item - used for <see cref="BaseItem.TypeName"/>.</param>
    /// <returns>The <see cref="AbstractValidator{TItem}"/>.</returns>
    private static AbstractValidator<TItem> CreateBaseItemValidator(
        string typeName)
    {
        var baseItemValidator = new InlineValidator<TItem>();

        // id
        baseItemValidator.RuleFor(k => k.Id)
            .NotEmpty()
            .WithMessage("Id is null or empty.");

        // partitionKey
        baseItemValidator.RuleFor(k => k.PartitionKey)
            .NotEmpty()
            .WithMessage("PartitionKey is null or empty.");

        // type
        baseItemValidator.RuleFor(k => k.TypeName)
            .Must(k => k == typeName)
            .WithMessage($"TypeName is not '{typeName}'.");

        // createdDate
        baseItemValidator.RuleFor(k => k.CreatedDate)
            .NotDefault()
            .WithMessage($"CreatedDate is not valid.");

        // updatedDate
        baseItemValidator.RuleFor(k => k.UpdatedDate)
            .NotDefault()
            .WithMessage($"UpdatedDate is not valid.");

        return baseItemValidator;
    }
}
