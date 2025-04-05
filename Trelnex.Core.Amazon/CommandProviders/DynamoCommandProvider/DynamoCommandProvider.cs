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

namespace Trelnex.Core.Amazon.CommandProviders;

/// <summary>
/// An implementation of <see cref="ICommandProvider{TInterface}"/> that uses a DynamoDB table as a backing store.
/// </summary>
internal class DynamoCommandProvider<TInterface, TItem>(
    Table table,
    string typeName,
    IValidator<TItem>? validator = null,
    CommandOperations? commandOperations = null)
    : CommandProvider<TInterface, TItem>(typeName, validator, commandOperations)
    where TInterface : class, IBaseItem
    where TItem : BaseItem, TInterface, new()
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly PropertyInfo _etagProperty =
        typeof(TItem).GetProperty(
            nameof(BaseItem.ETag),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

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
        return JsonSerializer.Deserialize<TItem>(json, _jsonSerializerOptions);
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

            // round-trip the item to update the etag
            var responseItem = JsonSerializer.Deserialize<TItem>(
                json: JsonSerializer.Serialize(request.Item, _jsonSerializerOptions),
                options: _jsonSerializerOptions);

            _etagProperty.SetValue(responseItem, Guid.NewGuid().ToString());

            results[index] = new SaveResult<TInterface, TItem>(
                HttpStatusCode: HttpStatusCode.OK,
                Item: responseItem);

            // serialize the item to the document
            // use responseItem to save the updated ETag
            var jsonItem = JsonSerializer.Serialize(responseItem, _jsonSerializerOptions);
            var documentItem = Document.FromJson(jsonItem);

            batch.AddDocumentToUpdate(documentItem, config);

            // serialize the event to the document
            var jsonEvent = JsonSerializer.Serialize(request.Event, _jsonSerializerOptions);
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

    /// <summary>
    /// Create the <see cref="IQueryable{TItem}"/> to query the items.
    /// </summary>
    /// <returns></returns>
    protected override IQueryable<TItem> CreateQueryable()
    {
        // add typeName and isDeleted predicates
        // the lambda parameter i is an item of TInterface type
        return Enumerable.Empty<TItem>()
            .AsQueryable()
            .Where(i => i.TypeName == TypeName)
            .Where(i => i.IsDeleted == null || i.IsDeleted == false);
    }

    /// <summary>
    /// Execute the query specified by the <see cref="IQueryable{TItem}"/> and return the results as an enumerable.
    /// </summary>
    /// <param name="queryable">The queryable.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The <see cref="IEnumerable{TInterface}"/>.</returns>
    protected override IEnumerable<TItem> ExecuteQueryable(
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
                var documents = search.GetNextSetAsync(cancellationToken).Result;

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
        return queryHelper.Filter(items);
    }
}
