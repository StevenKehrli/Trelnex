using System.Net;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Results;
using Trelnex.Core.Validation;

namespace Trelnex.Core.Data;

/// <summary>
/// Defines the contract for operations against a backing data store using the Command pattern.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <remarks>
/// <para>
/// This interface provides a standardized way to interact with different types of data stores
/// (e.g., in-memory, SQL, Cosmos DB) through a command-based pattern, abstracting away the
/// underlying implementation details.
/// </para>
/// <para>
/// The command-based design separates command creation from execution, allowing for validation
/// and modification before persistence. This separation enables consistent behavior across
/// different storage mechanisms and provides a unified approach to validation, concurrency control,
/// and error handling.
/// </para>
/// <para>
/// Commands support different operation types (Create, Read, Update, Delete, Query, and Batch)
/// and follow a soft-delete pattern where items are marked as deleted rather than physically removed.
/// This allows for data recovery and maintains a complete historical record.
/// </para>
/// <para>
/// Each item in the data store is identified by a combination of id and partition key, and
/// includes metadata such as type name, creation date, and update date that are automatically
/// managed by the provider.
/// </para>
/// </remarks>
public interface ICommandProvider<TInterface>
    where TInterface : class, IBaseItem
{
    /// <summary>
    /// Creates a batch command for executing multiple operations against the backing data store.
    /// </summary>
    /// <returns>
    /// An <see cref="IBatchCommand{TInterface}"/> that can be used to batch multiple commands
    /// for execution in a single transaction.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Batch commands allow multiple save operations (create, update, delete) to be executed
    /// in a single atomic transaction. This ensures that either all operations succeed or
    /// all fail together, maintaining data consistency.
    /// </para>
    /// <para>
    /// All operations in a batch must share the same partition key to ensure they can be
    /// executed atomically by the underlying data store. The batch command validates partition
    /// key consistency before execution.
    /// </para>
    /// <para>
    /// If any operation in the batch fails, the entire batch fails with appropriate error
    /// information for each operation, typically using HTTP status codes like 424 (Failed Dependency).
    /// </para>
    /// </remarks>
    IBatchCommand<TInterface> Batch();

    /// <summary>
    /// Creates a new item with the specified identifier and partition key.
    /// </summary>
    /// <param name="id">The unique identifier for the item.</param>
    /// <param name="partitionKey">The partition key determining the data partition for the item.</param>
    /// <returns>An <see cref="ISaveCommand{TInterface}"/> that wraps the newly created item and can be used to persist it.</returns>
    /// <remarks>
    /// <para>
    /// This method initializes a new item with the provided id and partition key, along with
    /// system-managed metadata like creation and update timestamps. It does not persist the
    /// item to the data store until the returned command is executed.
    /// </para>
    /// <para>
    /// The returned command object allows for further modification of the item before saving,
    /// and ensures validation occurs at execution time.
    /// </para>
    /// <para>
    /// Creation operations are always supported by command providers and cannot be disabled
    /// through the <see cref="CommandOperations"/> flags.
    /// </para>
    /// </remarks>
    ISaveCommand<TInterface> Create(
        string id,
        string partitionKey);

    /// <summary>
    /// Marks an existing item for deletion as an asynchronous operation.
    /// </summary>
    /// <param name="id">The unique identifier of the item to delete.</param>
    /// <param name="partitionKey">The partition key of the item to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an <see cref="ISaveCommand{TInterface}"/>
    /// with the item marked for deletion, or <see langword="null"/> if the item is not found or already deleted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method implements a soft-delete approach by marking the item with deletion metadata
    /// (setting IsDeleted=true and DeletedDate) rather than physically removing it from the data store.
    /// The item is not actually updated in the backing store until the returned command is executed.
    /// </para>
    /// <para>
    /// If the item doesn't exist or is already marked as deleted, this method returns null,
    /// indicating there is no action to perform.
    /// </para>
    /// <para>
    /// The returned command is read-only to prevent further modification of a to-be-deleted item.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// Thrown when the Delete operation is not supported by this provider as specified in <see cref="CommandOperations"/>.
    /// </exception>
    Task<ISaveCommand<TInterface>?> DeleteAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The unique identifier of the item to read.</param>
    /// <param name="partitionKey">The partition key of the item to read.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an <see cref="IReadResult{TInterface}"/>
    /// with the retrieved item, or <see langword="null"/> if the item is not found or has been deleted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves an item by its id and partition key. It automatically filters out
    /// soft-deleted items (where IsDeleted=true), so those will return null as if they don't exist.
    /// </para>
    /// <para>
    /// The returned read result provides validated access to the item and read-only operations
    /// to prevent unintended modifications. To modify the item, use the <see cref="UpdateAsync"/> method.
    /// </para>
    /// <para>
    /// Read operations are always supported by command providers and cannot be disabled
    /// through the <see cref="CommandOperations"/> flags.
    /// </para>
    /// </remarks>
    Task<IReadResult<TInterface>?> ReadAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Creates a LINQ query to retrieve items from the backing data store.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryCommand{TInterface}"/> that provides LINQ query capabilities
    /// against the backing data store.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Query commands enable LINQ-based querying against the data store with deferred execution.
    /// The query is not executed until methods like Execute or ToList are called on the returned query command.
    /// </para>
    /// <para>
    /// By default, queries automatically filter out deleted items (where IsDeleted=true) and
    /// only return items matching the provider's type name. Additional filtering, sorting,
    /// and projection can be applied using standard LINQ syntax.
    /// </para>
    /// <para>
    /// The query system uses expression conversion to transform LINQ expressions written against
    /// the interface type into expressions that work with the concrete implementation type.
    /// </para>
    /// </remarks>
    IQueryCommand<TInterface> Query();

    /// <summary>
    /// Prepares an existing item for update as an asynchronous operation.
    /// </summary>
    /// <param name="id">The unique identifier of the item to update.</param>
    /// <param name="partitionKey">The partition key of the item to update.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an <see cref="ISaveCommand{TInterface}"/>
    /// with the item to be updated, or <see langword="null"/> if the item is not found or has been deleted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method retrieves an existing item and prepares it for update. The item is not
    /// modified in the backing store until the returned command is executed, allowing for
    /// validation before persistence.
    /// </para>
    /// <para>
    /// When executed, the update command automatically sets the UpdatedDate property to the current time.
    /// All other property changes must be made explicitly before executing the command.
    /// </para>
    /// <para>
    /// If the item doesn't exist or is marked as deleted, this method returns null,
    /// indicating there is no item to update.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// Thrown when the Update operation is not supported by this provider as specified in <see cref="CommandOperations"/>.
    /// </exception>
    Task<ISaveCommand<TInterface>?> UpdateAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstract base implementation of <see cref="ICommandProvider{TInterface}"/> that provides
/// common functionality for different data store implementations.
/// </summary>
/// <typeparam name="TInterface">The interface type of the items in the backing data store.</typeparam>
/// <typeparam name="TItem">The concrete implementation type of the items that implements <typeparamref name="TInterface"/>.</typeparam>
/// <remarks>
/// <para>
/// This base class implements the Command pattern for data access operations, handling common
/// concerns such as validation, command creation, operation type checking, and partition key consistency,
/// while delegating the actual data store operations to derived classes through protected abstract methods.
/// </para>
/// <para>
/// The design separates the command creation from command execution, allowing for validation
/// to occur before any data is persisted. Derived classes need only implement the core storage
/// operations (<see cref="ReadItemAsync"/>, <see cref="SaveBatchAsync"/>, <see cref="CreateQueryable"/>,
/// and <see cref="ExecuteQueryable"/>), while this base class handles the command lifecycle.
/// </para>
/// <para>
/// CommandProvider supports both individual and batch operations, with built-in validation and
/// soft-delete functionality. Operations can be restricted through the <see cref="CommandOperations"/>
/// parameter to control which actions are permitted on the data store.
/// </para>
/// <para>
/// Item type names follow strict naming conventions enforced by the provider (lowercase letters
/// and hyphens, starting and ending with a letter), ensuring consistency across the data store.
/// Base item properties are automatically validated, and additional domain-specific validation
/// can be applied through the optional validator parameter.
/// </para>
/// </remarks>
public abstract partial class CommandProvider<TInterface, TItem>
    : ICommandProvider<TInterface>
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Protected Fields

    /// <summary>
    /// The type name of the item stored in the data store.
    /// </summary>
    /// <remarks>
    /// This value is used to identify the type of entity in the data store
    /// and is stored in the <see cref="BaseItem.TypeName"/> property.
    /// </remarks>
    protected readonly string _typeName;

    #endregion

    #region Private Fields

    /// <summary>
    /// The fluent validator for the base item properties.
    /// </summary>
    /// <remarks>
    /// This validator ensures that fundamental properties like Id and PartitionKey are valid.
    /// </remarks>
    private readonly IValidator<TItem> _baseItemValidator;

    /// <summary>
    /// Defines which command operations (Create, Update, Delete) are allowed by this provider.
    /// </summary>
    private readonly CommandOperations _commandOperations;

    /// <summary>
    /// Converts expressions from interface type to concrete implementation type.
    /// </summary>
    /// <remarks>
    /// Allows queries to be written against the interface type but executed against the concrete type.
    /// </remarks>
    private readonly ExpressionConverter<TInterface, TItem> _expressionConverter;

    /// <summary>
    /// The fluent validator for the specific item type.
    /// </summary>
    /// <remarks>
    /// This optional validator provides domain-specific validation rules for the item type.
    /// </remarks>
    private readonly IValidator<TItem>? _itemValidator;

    #endregion

    #region Protected Properties

    /// <summary>
    /// Gets the type name of the item stored in the data store.
    /// </summary>
    /// <value>
    /// The type name string used for <see cref="BaseItem.TypeName"/>.
    /// </value>
    protected string TypeName => _typeName;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandProvider{TInterface, TItem}"/> class.
    /// </summary>
    /// <param name="typeName">The type name of the item to be stored in the <see cref="BaseItem.TypeName"/> property.</param>
    /// <param name="validator">The optional validator for domain-specific validation of the item type.</param>
    /// <param name="commandOperations">The operations allowed by this provider. Defaults to Update only if not specified.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="typeName"/> does not follow the naming rules (lowercase letters and hyphens,
    /// starting and ending with a letter) or when it is a reserved type name.
    /// </exception>
    /// <remarks>
    /// The constructor sets up validators, command delegates, and verifies that the type name follows required conventions.
    /// </remarks>
    protected CommandProvider(
        string typeName,
        IValidator<TItem>? validator,
        CommandOperations? commandOperations = null)
    {
        // Validate the type name against the naming rules (lowercase letters and hyphens).
        // This ensures consistent naming conventions across all data types in the system.
        if (TypeRulesRegex().IsMatch(typeName) is false)
        {
            throw new ArgumentException($"The typeName '{typeName}' does not follow the naming rules: lowercase letters and hyphens; start and end with a lowercase letter.", nameof(typeName));
        }

        // Ensure the type name doesn't conflict with any system-reserved names.
        // Reserved names are protected to prevent conflicts with internal system functionality.
        if (ReservedTypeNames.IsReserved(typeName))
        {
            throw new ArgumentException($"The typeName '{typeName}' is a reserved type name.", nameof(typeName));
        }

        // Store the validated type name for use in item creation and queries.
        _typeName = typeName;

        // Set up validators for both base properties and custom item properties.
        // The base validator ensures core properties like Id and PartitionKey are valid,
        // while the custom validator adds domain-specific validation rules.
        _baseItemValidator = CreateBaseItemValidator(typeName);
        _itemValidator = validator;

        // Create the expression converter for LINQ operations.
        // This enables strongly-typed LINQ queries against the interface type
        // while working with the concrete implementation internally.
        _expressionConverter = new();

        // Set the allowed operations for this provider, defaulting to Update only if not specified.
        // This controls which operations (Update/Delete) are permitted on this data store.
        _commandOperations = commandOperations ?? CommandOperations.Update;
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
        // Create a timestamp for both creation and update fields.
        var dateTimeUtcNow = DateTime.UtcNow;

        // Initialize a new item with the specified id, partition key, and system metadata.
        var item = new TItem
        {
            Id = id,
            PartitionKey = partitionKey,

            TypeName = _typeName,

            CreatedDate = dateTimeUtcNow,
            UpdatedDate = dateTimeUtcNow,
        };

        // Create a save command that will validate and persist the item when executed.
        // The command is not read-only, allowing properties to be modified before execution.
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
        // Check if the delete operation is supported by this provider's configuration.
        // Not all data stores support deletion, based on the CommandOperations setting.
        if (_commandOperations.HasFlag(CommandOperations.Delete) is false)
        {
            throw new NotSupportedException("The requested Delete operation is not supported.");
        }

        // Read the existing item to ensure it exists and is not already deleted.
        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        // Return null if the item doesn't exist or is already marked as deleted.
        // This indicates there is no action to perform and is not considered an error.
        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        // Create a command that will mark the item as deleted when executed.
        return CreateDeleteCommand(item);
    }

    /// <inheritdoc />
    public IQueryCommand<TInterface> Query()
    {
        // Create the queryable that will be used to query the backing data store.
        // This builds the base queryable with type and soft-delete filtering already applied.
        var queryable = CreateQueryable();

        // Define a function to convert raw items to query results with command capabilities.
        // This enables returned items to be updated or deleted directly from query results.
        Func<TItem, IQueryResult<TInterface>> convertToQueryResult = item => {
            return QueryResult<TInterface, TItem>.Create(
                item: item,
                validateAsyncDelegate: ValidateAsync,
                createDeleteCommand: CreateDeleteCommand,
                createUpdateCommand: CreateUpdateCommand);
        };

        // Create the query command with all necessary components for executing queries.
        // This includes the expression converter for interface-to-concrete-type translation.
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
        // Retrieve the item from the backing store using the implementation-specific read method.
        // This delegates to the derived class's ReadItemAsync method for the actual storage operation.
        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        // If the item doesn't exist or is marked as deleted, return null.
        // This implements the soft-delete pattern, making deleted items appear as if they don't exist.
        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        // Create and return a read result that wraps the item.
        // The read result provides read-only access and validation of the item.
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
        // Check if the update operation is supported by this provider's configuration.
        // This enforces the operation permissions defined when the provider was created.
        if (_commandOperations.HasFlag(CommandOperations.Update) is false)
        {
            throw new NotSupportedException("The requested Update operation is not supported.");
        }

        // Read the existing item to ensure it exists and is not deleted.
        // This uses the provider's own ReadItemAsync method to retrieve the current item state.
        var item = await ReadItemAsync(id, partitionKey, cancellationToken);

        // Return null if the item doesn't exist or is marked as deleted.
        // This provides a consistent response pattern for non-existent resources.
        if (item is null || (item.IsDeleted ?? false))
        {
            return null;
        }

        // Create a command that will update the item when executed.
        // This sets the UpdatedDate timestamp and creates a SaveCommand with UPDATED action.
        return CreateUpdateCommand(item);
    }

    #endregion

    #region Protected Abstract Methods

    /// <summary>
    /// Creates an <see cref="IQueryable{TItem}"/> for querying items in the backing data store.
    /// </summary>
    /// <returns>
    /// An <see cref="IQueryable{TItem}"/> that can be used to build LINQ queries against the data store.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This abstract method must be implemented by derived classes to create a queryable instance
    /// specific to the underlying data store technology (e.g., SQL, NoSQL, in-memory).
    /// </para>
    /// <para>
    /// The implementation should apply basic filters like type name matching and soft-delete
    /// filtering, to ensure consistent behavior across all query operations.
    /// </para>
    /// </remarks>
    protected abstract IQueryable<TItem> CreateQueryable();

    /// <summary>
    /// Executes a LINQ query against the backing data store and materializes the results.
    /// </summary>
    /// <param name="queryable">The LINQ query to execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// An enumerable of items matching the query.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This abstract method must be implemented by derived classes to execute the provided
    /// queryable expression against the specific data store technology.
    /// </para>
    /// <para>
    /// The implementation should handle translation of LINQ expressions to the appropriate
    /// query language, execution, and materializing results into strongly-typed objects.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    protected abstract IEnumerable<TItem> ExecuteQueryable(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an item from the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="id">The unique identifier of the item to read.</param>
    /// <param name="partitionKey">The partition key of the item to read.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the item if found,
    /// or <see langword="null"/> if the item does not exist.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This abstract method must be implemented by derived classes to perform the actual read
    /// operation against the specific data store technology.
    /// </para>
    /// <para>
    /// The implementation should retrieve the item based on its id and partition key without
    /// applying any soft-delete filtering, as that's handled by the base class.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    protected abstract Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a batch of items in the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="requests">An array of save requests, each containing an item and its associated event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an array of
    /// <see cref="SaveResult{TInterface, TItem}"/> objects, one for each request in the batch.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This abstract method must be implemented by derived classes to perform the actual batch save
    /// operation against the specific data store technology.
    /// </para>
    /// <para>
    /// The implementation should ensure that all operations within the batch are executed
    /// atomically - either all succeed or all fail together.
    /// </para>
    /// <para>
    /// All items in the batch must have the same partition key to support atomic execution,
    /// which is validated by the base class before this method is called.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    protected abstract Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default);

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a command to mark an item as deleted.
    /// </summary>
    /// <param name="item">The item to mark as deleted.</param>
    /// <returns>
    /// An <see cref="ISaveCommand{TInterface}"/> that can be executed to perform the deletion.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method modifies the item by setting the DeletedDate to the current time
    /// and the IsDeleted flag to <see langword="true"/>.
    /// </para>
    /// <para>
    /// The created command is read-only to prevent further modifications to an item
    /// that is about to be deleted.
    /// </para>
    /// </remarks>
    private ISaveCommand<TInterface> CreateDeleteCommand(
        TItem item)
    {
        // Set the deletion timestamp to the current UTC time.
        // This records when the item was marked for deletion.
        item.DeletedDate = DateTime.UtcNow;

        // Mark the item as deleted to support soft-delete functionality.
        // This flag allows the system to filter out deleted items from queries.
        item.IsDeleted = true;

        // Create a read-only command that will save the item with deletion markers when executed.
        // The command is read-only to prevent further modification of a to-be-deleted item.
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
    /// <remarks>
    /// <para>
    /// This method modifies the item by setting the UpdatedDate to the current time.
    /// </para>
    /// <para>
    /// The created command allows for further modifications to the item before it is
    /// persisted to the data store.
    /// </para>
    /// </remarks>
    private ISaveCommand<TInterface> CreateUpdateCommand(
        TItem item)
    {
        // Update the timestamp to track when the item was last modified.
        item.UpdatedDate = DateTime.UtcNow;

        // Create a modifiable command that will save the updated item when executed.
        // The command is not read-only, allowing properties to be further modified before execution.
        return SaveCommand<TInterface, TItem>.Create(
            item: item,
            isReadOnly: false,
            validateAsyncDelegate: ValidateAsync,
            saveAction: SaveAction.UPDATED,
            saveAsyncDelegate: SaveItemAsync);
    }

    /// <summary>
    /// Saves a single item in the backing data store as an asynchronous operation.
    /// </summary>
    /// <param name="request">The save request containing the item and its associated event.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the saved item.
    /// </returns>
    /// <exception cref="CommandException">
    /// Thrown when the save operation fails, with the HTTP status code from the underlying operation.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method wraps the single save operation as a batch operation with one item,
    /// allowing for consistent handling of save operations.
    /// </para>
    /// <para>
    /// This approach provides a unified error handling mechanism for both single and batch operations.
    /// </para>
    /// </remarks>
    private async Task<TItem> SaveItemAsync(
        SaveRequest<TInterface, TItem> request,
        CancellationToken cancellationToken = default)
    {
        // Wrap the single save request as a batch of one for consistent processing.
        // This allows reuse of batch save logic for single item operations.
        SaveRequest<TInterface, TItem>[] requests = [ request ];

        // Execute the save operation using the batch implementation.
        // This delegates to the storage-specific implementation in the derived class.
        var results = await SaveBatchAsync(
            requests: requests,
            cancellationToken: cancellationToken);

        // Extract the result for our single item from the batch results.
        var result = results[0];

        // If the save was not successful (status code not OK), throw an exception.
        // This propagates errors from the storage layer with appropriate HTTP status codes.
        if (result.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new CommandException(result.HttpStatusCode);
        }

        // Return the successfully saved item.
        // This includes any server-side changes like generated ETags or timestamps.
        return result.Item!;
    }

    /// <summary>
    /// Validates an item using the base item validator and the item-specific validator.
    /// </summary>
    /// <param name="item">The item to validate.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the validation result,
    /// which indicates whether validation was successful and any validation errors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method combines the base validation rules (for common properties) with any
    /// item-specific validation rules provided through the item validator.
    /// </para>
    /// <para>
    /// The validation is performed asynchronously to support complex validation rules
    /// that may require external resource access.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the operation is canceled through the cancellation token.
    /// </exception>
    private async Task<ValidationResult> ValidateAsync(
        TItem item,
        CancellationToken cancellationToken)
    {
        // Create a composite of the base item validator and the item validator.
        // This ensures both system requirements and domain-specific rules are applied.
        var compositeValidator = new CompositeValidator<TItem>(_baseItemValidator, _itemValidator);

        // Validate the item against all applicable validation rules.
        return await compositeValidator.ValidateAsync(item, cancellationToken);
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Creates a validator for the base properties of all items.
    /// </summary>
    /// <param name="typeName">The expected type name for all items.</param>
    /// <returns>An <see cref="IValidator{TItem}"/> for validating base item properties.</returns>
    /// <remarks>
    /// <para>
    /// This validator ensures that all required base properties (Id, PartitionKey, TypeName,
    /// CreatedDate, UpdatedDate) are valid according to system requirements.
    /// </para>
    /// <para>
    /// The validation rules are applied consistently across all item types in the system.
    /// </para>
    /// </remarks>
    private static IValidator<TItem> CreateBaseItemValidator(
        string typeName)
    {
        // Create a new validator for the base item properties.
        var baseItemValidator = new InlineValidator<TItem>();

        // Ensure the Id is provided and not empty.
        // The id is required for uniquely identifying the item within its partition.
        baseItemValidator.RuleFor(k => k.Id)
            .NotEmpty()
            .WithMessage("Id is null or empty.");

        // Ensure the PartitionKey is provided and not empty.
        // The partition key determines how data is distributed across storage nodes.
        baseItemValidator.RuleFor(k => k.PartitionKey)
            .NotEmpty()
            .WithMessage("PartitionKey is null or empty.");

        // Ensure the TypeName matches the expected type name for this provider.
        // This prevents items from different providers being mixed incorrectly.
        baseItemValidator.RuleFor(k => k.TypeName)
            .Must(k => k == typeName)
            .WithMessage($"TypeName is not '{typeName}'.");

        // Ensure CreatedDate has a valid value (not default/empty).
        // This timestamp tracks when the item was first created.
        baseItemValidator.RuleFor(k => k.CreatedDate)
            .NotDefault()
            .WithMessage("CreatedDate is not valid.");

        // Ensure UpdatedDate has a valid value (not default/empty).
        // This timestamp tracks when the item was last modified.
        baseItemValidator.RuleFor(k => k.UpdatedDate)
            .NotDefault()
            .WithMessage("UpdatedDate is not valid.");

        return baseItemValidator;
    }

    /// <summary>
    /// Creates a regular expression that enforces the naming rules for type names.
    /// </summary>
    /// <returns>A <see cref="Regex"/> that matches valid type names.</returns>
    /// <remarks>
    /// <para>
    /// Valid type names consist of lowercase letters and hyphens, must start and end with a lowercase letter,
    /// and must contain at least two characters.
    /// </para>
    /// <para>
    /// This pattern ensures consistent naming across all data types in the system and
    /// prevents problematic characters in type names.
    /// </para>
    /// </remarks>
    [GeneratedRegex(@"^[a-z]+[a-z-]*[a-z]+$")]
    private static partial Regex TypeRulesRegex();

    #endregion
}
