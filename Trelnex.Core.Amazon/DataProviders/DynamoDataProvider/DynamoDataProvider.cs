using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// DynamoDB implementation of data provider for storing and retrieving items.
/// </summary>
/// <param name="typeName">Type name identifier for filtering items.</param>
/// <param name="itemTable">DynamoDB table instance for data operations.</param>
/// <param name="eventTable">DynamoDB table instance for event tracking, or null if EventPolicy is Disabled.</param>
/// <param name="itemValidator">Optional validator for items before saving.</param>
/// <param name="commandOperations">Optional CRUD operations override.</param>
/// <param name="eventPolicy">Optional event policy for change tracking.</param>
/// <param name="eventTimeToLive">Optional TTL for events in seconds.</param>
/// <param name="blockCipherService">Optional encryption service for sensitive data.</param>
/// <param name="logger">Optional logger for diagnostics.</param>
internal class DynamoDataProvider<TItem>(
    string typeName,
    Table itemTable,
    Table? eventTable,
    IValidator<TItem>? itemValidator = null,
    CommandOperations? commandOperations = null,
    EventPolicy? eventPolicy = null,
    int? eventTimeToLive = null,
    IBlockCipherService? blockCipherService = null,
    ILogger? logger = null)
    : DataProvider<TItem>(
        typeName: typeName,
        itemValidator: itemValidator,
        commandOperations: commandOperations,
        eventPolicy: eventPolicy,
        blockCipherService: blockCipherService,
        logger: logger)
    where TItem : BaseItem, new()
{
    #region Private Static Fields

    // Reflection access to ETag property for setting generated values
    private static readonly PropertyInfo _etagProperty =
        typeof(TItem).GetProperty(
            nameof(BaseItem.ETag),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

    #endregion

    #region Protected Methods

    /// <summary>
    /// Creates an empty queryable with standard filters for DynamoDB translation.
    /// </summary>
    /// <returns>Empty queryable with type and deletion status filters.</returns>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Create an empty in-memory queryable with standard predicates
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Executes a LINQ query against DynamoDB by translating it to scan operations.
    /// </summary>
    /// <param name="queryable">LINQ queryable to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Asynchronous enumerable of matching items.</returns>
    /// <exception cref="CommandException">Thrown when DynamoDB operations fail.</exception>
    [TraceMethod]
    protected override async IAsyncEnumerable<IQueryResult<TItem>> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Convert LINQ expression to DynamoDB scan with filtering
        var queryHelper = QueryHelper<TItem>.FromLinqExpression(queryable.Expression);

        // Execute DynamoDB scan with converted expression
        var search = itemTable.Scan(queryHelper.DynamoWhereExpression);

        var items = new List<TItem>();

        do
        {
            try
            {
                // Retrieve next batch of documents from DynamoDB
                var documents = await search.GetNextSetAsync(cancellationToken);

                // Convert each document to typed item
                documents.ForEach(document =>
                {
                    var json = document.ToJson();
                    var item = DeserializeItem(json)!;

                    items.Add(item);
                });
            }
            catch (AggregateException ex) when (ex.InnerException is AmazonDynamoDBException ade)
            {
                var httpStatusCode = ConvertReasonCode(ade.ErrorCode);

                throw new CommandException(httpStatusCode, ade.Message, ade);
            }
            catch (AmazonDynamoDBException ade)
            {
                var httpStatusCode = ConvertReasonCode(ade.ErrorCode);

                throw new CommandException(httpStatusCode, ade.Message, ade);
            }
        } while (search.IsDone is false);

        // Apply remaining LINQ filters to retrieved items
        foreach (var item in queryHelper.Filter(items))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var queryResult = ConvertToQueryResult(item);

            yield return queryResult;
        }
    }

    /// <summary>
    /// Retrieves a single item from DynamoDB using composite primary key.
    /// </summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="partitionKey">Partition key for the item.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Item if found and matches type, null otherwise.</returns>
    /// <exception cref="CommandException">Thrown when DynamoDB operations fail.</exception>
    [TraceMethod]
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        // Build composite key for DynamoDB GetItem operation
        var key = new Dictionary<string, DynamoDBEntry>
        {
            { "partitionKey", partitionKey },
            { "id", id }
        };

        var document = await itemTable.GetItemAsync(key, cancellationToken);

        if (document is null) return null;

        // Convert document to JSON and deserialize to typed item
        var json = document.ToJson();
        var item = DeserializeItem(json);

        // Verify item matches expected type name
        return item?.TypeName == TypeName
            ? item
            : null;
    }

    /// <summary>
    /// Saves multiple items and events atomically using DynamoDB transactions.
    /// </summary>
    /// <param name="requests">Array of save requests to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Array of save results for each request.</returns>
    /// <exception cref="CommandException">Thrown when DynamoDB operations fail.</exception>
    [TraceMethod]
    protected override async Task<SaveResult<TItem>[]> SaveBatchAsync(
        SaveRequest<TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // Initialize results array for all requests
        var results = new SaveResult<TItem>[requests.Length];

        // Create DynamoDB transaction batch
        var itemBatch = itemTable.CreateTransactWrite();
        var eventBatch = eventTable?.CreateTransactWrite();

        // Process each request and add to transaction
        for (var index = 0; index < requests.Length; index++)
        {
            var request = requests[index];

            // Build conditional expression for optimistic concurrency control
            var expressionStatement = request.Item.ETag is null
                ? "(attribute_not_exists(partitionKey) AND attribute_not_exists(id))"
                : "(attribute_exists(partitionKey) AND attribute_exists(id) AND #etag = :_etag)";

            var expressionAttributeNames = new Dictionary<string, string>();
            if (request.Item.ETag != null) expressionAttributeNames["#etag"] = "_etag";

            var expressionAttributeValues = new Dictionary<string, DynamoDBEntry>();
            if (request.Item.ETag != null) expressionAttributeValues[":_etag"] = request.Item.ETag;

            var config = new TransactWriteItemOperationConfig
            {
                ConditionalExpression = new Expression
                {
                    ExpressionAttributeNames = expressionAttributeNames,
                    ExpressionAttributeValues = expressionAttributeValues,
                    ExpressionStatement = expressionStatement
                }
            };

            var etag = Guid.NewGuid().ToString();

            // Create response item with new ETag
            var responseItem = request.Item with { };
            _etagProperty.SetValue(responseItem, etag);

            results[index] = new SaveResult<TItem>(
                HttpStatusCode: HttpStatusCode.OK,
                Item: responseItem);

            // Serialize item and add to transaction
            var jsonItem = SerializeItem(responseItem);
            var documentItem = Document.FromJson(jsonItem);

            itemBatch.AddDocumentToUpdate(documentItem, config);

            // Skip if no event to record
            if (request.Event is null) continue;

            // Calculate event expiration and create event with same ETag
            var eventExpireAt = (eventTimeToLive is null)
                ? null
                : request.Event.CreatedDateTimeOffset.ToUnixTimeSeconds() + eventTimeToLive;
            var responseEvent = new ItemEventWithExpiration(request.Event, eventExpireAt);
            _etagProperty.SetValue(responseEvent, etag);

            // Serialize event and add to transaction
            var jsonEvent = SerializeEvent(responseEvent);
            var documentEvent = Document.FromJson(jsonEvent);

            eventBatch?.AddDocumentToUpdate(documentEvent);
        }

        try
        {
            // Execute the entire transactions atomically
            var batches = eventBatch is not null
                ? new[] { itemBatch, eventBatch }
                : new[] { itemBatch };

            var multiTableBatch = new MultiTableDocumentTransactWrite(batches);

            await multiTableBatch.ExecuteAsync(cancellationToken);
        }
        catch (TransactionCanceledException ex)
        {
            var cancellationReasons = ex.CancellationReasons;

            // Update results with failure reasons for each cancelled operation
            // The item batch cancellation reasons are first in the array
            // since that corresponds to the order of items in the transaction
            for (var index = 0; index < requests.Length; index++)
            {
                var cancellationReason = cancellationReasons[index];
                var httpStatusCode = ConvertReasonCode(cancellationReason.Code);

                if (httpStatusCode == HttpStatusCode.OK) continue;

                results[index] = new SaveResult<TItem>(
                    HttpStatusCode: httpStatusCode,
                    Item: requests[index].Item);
            }
        }

        return results;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Maps DynamoDB error codes to appropriate HTTP status codes.
    /// </summary>
    /// <param name="code">DynamoDB error code string.</param>
    /// <returns>Corresponding HTTP status code.</returns>
    private static HttpStatusCode ConvertReasonCode(
        string code)
    {
        return code switch
        {
            "None" => HttpStatusCode.OK,

            "AccessDenied" => HttpStatusCode.Forbidden,
            "ConditionalCheckFailed" => HttpStatusCode.PreconditionFailed,
            "DuplicateItem" => HttpStatusCode.Conflict,
            "InternalServerError" => HttpStatusCode.InternalServerError,
            "ItemCollectionSizeLimitExceeded" => HttpStatusCode.RequestEntityTooLarge,
            "ProvisionedThroughputExceededException" => HttpStatusCode.ServiceUnavailable,
            "RequestLimitExceeded" => HttpStatusCode.TooManyRequests,
            "ResourceNotFound" => HttpStatusCode.NotFound,
            "ThrottlingError" => HttpStatusCode.ServiceUnavailable,
            "TransactionConflict" => HttpStatusCode.Conflict,
            "ValidationError" => HttpStatusCode.BadRequest,

            _ => HttpStatusCode.InternalServerError
        };
    }

    #endregion
}
