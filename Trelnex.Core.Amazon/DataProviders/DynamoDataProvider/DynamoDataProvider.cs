using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using FluentValidation;
using Trelnex.Core.Data;
using Trelnex.Core.Encryption;
using Trelnex.Core.Observability;

namespace Trelnex.Core.Amazon.DataProviders;

/// <summary>
/// DynamoDB implementation of <see cref="DataProvider{TInterface, TItem}"/>.
/// </summary>
/// <param name="table">The DynamoDB table to interact with.</param>
/// <param name="typeName">The type name used to filter items.</param>
/// <param name="validator">Optional validator for items before they are saved.</param>
/// <param name="commandOperations">Optional command operations to override default behaviors.</param>
/// <param name="blockCipherService">Optional block cipher service for encrypting sensitive data.</param>
internal class DynamoDataProvider<TInterface, TItem>(
    Table table,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null,
    IBlockCipherService? blockCipherService = null)
    : DataProvider<TInterface, TItem>(typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    #region Private Static Fields

    /// <summary>
    /// Reflection access to the ETag property.
    /// </summary>
    private static readonly PropertyInfo _etagProperty =
        typeof(TItem).GetProperty(
            nameof(BaseItem.ETag),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

    #endregion

    #region Private Fields

    /// <summary>
    /// JSON serializer options for DynamoDB.
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = blockCipherService is not null
            ? new EncryptedJsonTypeInfoResolver(blockCipherService)
            : null
    };

    #endregion

    #region Protected Methods

    /// <summary>
    /// Creates an in-memory queryable for DynamoDB item filtering.
    /// </summary>
    /// <returns>A LINQ queryable.</returns>
    /// <remarks>
    /// Creates an in-memory queryable that will be translated into DynamoDB expressions.
    /// </remarks>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // Create an empty in-memory queryable with standard predicates
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Executes a query against DynamoDB and returns the results.
    /// </summary>
    /// <param name="queryable">The queryable to translate and execute.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An enumerable of items matching the query.</returns>
    /// <exception cref="CommandException">When a DynamoDB exception occurs.</exception>
    /// <remarks>
    /// Translates the LINQ expression into a DynamoDB scan operation.
    /// </remarks>
#pragma warning disable CS8425
    [TraceMethod]
    protected override async IAsyncEnumerable<TItem> ExecuteQueryableAsync(
        IQueryable<TItem> queryable,
        CancellationToken cancellationToken = default)
    {
        // convert the queryable into the DynamoDB Where expression and LINQ filter expressions
        var queryHelper = QueryHelper<TItem>.FromLinqExpression(queryable.Expression);

        // execute the scan using the DynamoDB Where expression
        var search = table.Scan(queryHelper.DynamoWhereExpression);

        var items = new List<TItem>();

        do
        {
            try
            {
                // get the next batch of documents
                var documents = await search.GetNextSetAsync(cancellationToken);

                // convert each documnent to the TItem
                documents.ForEach(document =>
                {
                    var json = document.ToJson();
                    var item = JsonSerializer.Deserialize<TItem>(json, _jsonSerializerOptions)!;

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

        // apply the remaining LINQ filter expressions
        foreach (var item in queryHelper.Filter(items))
        {
            cancellationToken.ThrowIfCancellationRequested();

            yield return item;
        }
    }
#pragma warning restore CS8425

    /// <summary>
    /// Reads an item from the DynamoDB table.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <param name="partitionKey">The partition key of the item.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The item if found, or <see langword="null"/> if the item does not exist.</returns>
    /// <exception cref="CommandException">When a DynamoDB exception occurs.</exception>
    /// <remarks>
    /// Uses the DynamoDB GetItem operation with a composite key.
    /// </remarks>
    [TraceMethod]
    protected override async Task<TItem?> ReadItemAsync(
        string id,
        string partitionKey,
        CancellationToken cancellationToken = default)
    {
        // get the document
        var key = new Dictionary<string, DynamoDBEntry>
        {
            { "partitionKey", partitionKey },
            { "id", id }
        };

        var document = await table.GetItemAsync(key, cancellationToken);

        if (document is null) return null;

        // convert to json
        var json = document.ToJson();

        // deserialize the item
        var item = JsonSerializer.Deserialize<TItem>(json, _jsonSerializerOptions);

        return item?.TypeName == TypeName
            ? item
            : null;
    }

    /// <summary>
    /// Saves a batch of items in DynamoDB as an atomic transaction.
    /// </summary>
    /// <param name="requests">Array of save requests.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Array of save results.</returns>
    /// <exception cref="CommandException">When a DynamoDB exception occurs.</exception>
    /// <remarks>
    /// Uses the DynamoDB TransactWriteItems operation to ensure all-or-nothing consistency.
    /// </remarks>
    [TraceMethod]
    protected override async Task<SaveResult<TInterface, TItem>[]> SaveBatchAsync(
        SaveRequest<TInterface, TItem>[] requests,
        CancellationToken cancellationToken = default)
    {
        // allocate the results
        var results = new SaveResult<TInterface, TItem>[requests.Length];

        // create the batch operation
        var batch = table.CreateTransactWrite();

        // add the items to the batch
        for (var index = 0; index < requests.Length; index++)
        {
            var request = requests[index];

            // create the conditional expression to check if the item can be saved
            // use the request.Item to check against ETag in the table
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

            // round-trip the item to update the etag
            var responseItem = JsonSerializer.Deserialize<TItem>(
                json: JsonSerializer.Serialize(request.Item, _jsonSerializerOptions),
                options: _jsonSerializerOptions);

            _etagProperty.SetValue(responseItem, etag);

            results[index] = new SaveResult<TInterface, TItem>(
                HttpStatusCode: HttpStatusCode.OK,
                Item: responseItem);

            // serialize the item to the document
            // use responseItem to save the updated ETag
            var jsonItem = JsonSerializer.Serialize(responseItem, _jsonSerializerOptions);
            var documentItem = Document.FromJson(jsonItem);

            batch.AddDocumentToUpdate(documentItem, config);

            // round-trip the event to update the etag
            var responseEvent = JsonSerializer.Deserialize<TItem>(
                json: JsonSerializer.Serialize(request.Event, _jsonSerializerOptions),
                options: _jsonSerializerOptions);

            _etagProperty.SetValue(responseEvent, etag);

            // serialize the event to the document
            // use responseEvent to save the updated ETag
            var jsonEvent = JsonSerializer.Serialize(responseEvent, _jsonSerializerOptions);
            var documentEvent = Document.FromJson(jsonEvent);

            batch.AddDocumentToUpdate(documentEvent);
        }

        try
        {
            // execute the batch
            await batch.ExecuteAsync(cancellationToken);
        }
        catch (TransactionCanceledException ex)
        {
            var cancellationReasons = ex.CancellationReasons;

            // enumerate the results and get its cancellation reason, if any
            for (var index = 0; index < requests.Length; index++)
            {
                var cancellationReason = cancellationReasons[index * 2];
                var httpStatusCode = ConvertReasonCode(cancellationReason.Code);

                if (httpStatusCode == HttpStatusCode.OK) continue;

                results[index] = new SaveResult<TInterface, TItem>(
                    HttpStatusCode: httpStatusCode,
                    Item: requests[index].Item);
            }
        }

        return results;
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Converts a DynamoDB error code to an HTTP status code.
    /// </summary>
    /// <param name="code">The DynamoDB error code string.</param>
    /// <returns>The mapped HTTP status code.</returns>
    /// <remarks>
    /// Maps DynamoDB-specific error codes to standard HTTP status codes.
    /// </remarks>
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
