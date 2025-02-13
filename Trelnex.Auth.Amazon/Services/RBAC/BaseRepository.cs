using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Trelnex.Core;

namespace Trelnex.Auth.Amazon.Services.RBAC;

internal abstract class BaseItem
{
    protected BaseItem(
        string resourceName)
    {
        ResourceName = resourceName;
    }

    protected BaseItem(
        Dictionary<string, AttributeValue> attributeMap)
    {
        ResourceName = attributeMap["resourceName"].S;
    }

    /// <summary>
    /// The name of the resource.
    /// </summary>
    public string ResourceName { get; init; }

    /// <summary>
    /// The name of the subject.
    /// </summary>
    public abstract string SubjectName { get; }

    public Dictionary<string, AttributeValue> Key => new()
    {
        { "resourceName", new AttributeValue(ResourceName) },
        { "subjectName", new AttributeValue(SubjectName) }
    };

    /// <summary>
    /// Converts the item to its attribute map
    /// </summary>
    /// <returns>The attribute map representing the item.</returns>
    public virtual Dictionary<string, AttributeValue> ToAttributeMap() => Key;
}

internal abstract class ScanItem
{
    private readonly Dictionary<string, AttributeValue> _attributeMap = [];

    public IReadOnlyDictionary<string, AttributeValue> AttributeMap => _attributeMap;

    public abstract string SubjectName { get; }

    public void AddResourceName(
        string resourceName)
    {
        AddAttribute("resourceName", resourceName);
    }

    protected void AddAttribute(
        string attributeName,
        string attributeValue)
    {
        _attributeMap.Add(attributeName, new AttributeValue(attributeValue));
    }
}

internal abstract class BaseRepository<TBaseItem>(
    AmazonDynamoDBClient client,
    string tableName)
    where TBaseItem : BaseItem
{
    /// <summary>
    /// Creates the specified item in the data source
    /// </summary>
    /// <param name="item">The specified item to create.
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created item.</returns>
    public Task CreateAsync(
        TBaseItem item,
        CancellationToken cancellationToken)
    {
        // create the request
        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = item.ToAttributeMap()
        };

        try
        {
            // put the item
            return client.PutItemAsync(request, cancellationToken);
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes the specified item from the data source
    /// </summary>
    /// <param name="item">The specified item to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteAsync(
        TBaseItem item,
        CancellationToken cancellationToken)
    {
        // create the request
        var request = new DeleteItemRequest
        {
            TableName = tableName,
            Key = item.Key
        };

        try
        {
            // delete the item
            await client.DeleteItemAsync(request, cancellationToken);
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes the batch of items from the data source
    /// </summary>
    /// <param name="items">The batch of items to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteAsync(
        TBaseItem[] items,
        CancellationToken cancellationToken)
    {
        if (items.Length is 0) return;

        // create the delete requests
        var deleteRequests = items.Select(item => new DeleteRequest { Key = item.Key });
        var writeRequests = deleteRequests.Select(deleteRequest => new WriteRequest { DeleteRequest = deleteRequest });

        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                { tableName, writeRequests.ToList() }
            }
        };

        try
        {
            // delete until complete
            while (batchWriteRequest.RequestItems.Count > 0)
            {
                // delete
                var response = await client.BatchWriteItemAsync(batchWriteRequest, cancellationToken);

                batchWriteRequest.RequestItems = response.UnprocessedItems;
            }
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Gets the specified item from the data source.
    /// </summary>
    /// <param name="item">The specified item to get.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The item if found; otherwise, <see langword="null"/>.</returns>
    public async Task<TBaseItem?> GetAsync(
        TBaseItem item,
        CancellationToken cancellationToken)
    {
        // create the get request
        var request = new GetItemRequest
        {
            TableName = tableName,
            Key = item.Key,
            ConsistentRead = true
        };

        try
        {
            // get
            var response = await client.GetItemAsync(request, cancellationToken);

            // convert the attribute map to the item
            return BaseRepository<TBaseItem>.FromAttributeMap(response.Item);
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Scans the data source for the specified items.
    /// </summary>
    /// <param name="item">The specified item to scan for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The array of items.</returns>
    public async Task<TBaseItem[]> ScanAsync(
        ScanItem scanItem,
        CancellationToken cancellationToken)
    {
        // build the collection of filter conditions
        // +1 to always include the subject name
        var filterConditions = new List<string>(scanItem.AttributeMap.Count + 1)
        {
            "begins_with(subjectName, :subjectName)"
        };

        // create the expression attribute values
        // always include the subject name
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":subjectName", new AttributeValue(scanItem.SubjectName) }
        };

        // enumerate the attribute map
        // add the filter condition and expression attribute value
        foreach (var kvp in scanItem.AttributeMap)
        {
            // add the filter condition
            // for example: "attributeName = :attributeName"
            filterConditions.Add(
                $"{kvp.Key} = :{kvp.Key}");

            // add the expression attribute value
            // for example: { ":attributeName", new AttributeValue(attributeValue) }
            expressionAttributeValues.Add(
                $":{kvp.Key}",
                kvp.Value);
        }

        var request = new ScanRequest()
        {
            TableName = tableName,
            FilterExpression = string.Join(" AND ", filterConditions),
            ExpressionAttributeValues = expressionAttributeValues,
            ConsistentRead = true
        };

        try
        {
            // scan
            var response = await client.ScanAsync(request, cancellationToken);

            // convert the response to the items
            var items = new List<TBaseItem>(response.Items.Count);
            foreach (var attributeMap in response.Items)
            {
                var responseItem = BaseRepository<TBaseItem>.FromAttributeMap(attributeMap);

                if (responseItem is not null) items.Add(responseItem);
            }

            return items.ToArray();
        }
        catch (AmazonDynamoDBException ex)
        {
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Converts the specified attribute map to the item.
    /// </summary>
    /// <param name="attributeMap">The attribute map to convert to the item.</param>
    /// <returns>The item if valid; otherwise, <see langword="null"/>.</returns>
    private static TBaseItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        try
        {
            // create the item
            var item = Activator.CreateInstance(typeof(TBaseItem), attributeMap) as TBaseItem;

            // get the subject name
            var subjectName = attributeMap["subjectName"].S;

            // validate the subject name
            return string.Equals(item?.SubjectName, subjectName)
                ? item
                : default;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is KeyNotFoundException)
        {
            return default;
        }
    }
}
