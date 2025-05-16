using System.Net;
using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Trelnex.Core;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Base class for items stored in the DynamoDB data source.
/// </summary>
/// <remarks>
/// Provides common functionality for all items in the RBAC system,
/// including resource name and subject name properties and DynamoDB attribute mapping.
/// </remarks>
internal abstract class BaseItem
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItem"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource associated with this item.</param>
    protected BaseItem(
        string resourceName)
    {
        // Set the resource name.
        ResourceName = resourceName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseItem"/> class from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map containing the item data.</param>
    /// <exception cref="KeyNotFoundException">Thrown when required attributes are missing from the map.</exception>
    protected BaseItem(
        Dictionary<string, AttributeValue> attributeMap)
    {
        // Get the resource name from the attribute map.
        ResourceName = attributeMap["resourceName"].S;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the name of the resource associated with this item.
    /// </summary>
    public string ResourceName { get; init; }

    /// <summary>
    /// Gets the name of the subject for this item.
    /// </summary>
    /// <remarks>
    /// The subject name is used as part of the composite key in DynamoDB.
    /// Each derived class must implement this property.
    /// </remarks>
    public abstract string SubjectName { get; }

    /// <summary>
    /// Gets the DynamoDB key for this item (resourceName and subjectName).
    /// </summary>
    /// <remarks>
    /// The key is used to uniquely identify the item in the DynamoDB table.
    /// </remarks>
    public Dictionary<string, AttributeValue> Key => new()
    {
        { "resourceName", new AttributeValue(ResourceName) },
        { "subjectName", new AttributeValue(SubjectName) }
    };

    #endregion

    #region Public Methods

    /// <summary>
    /// Converts the item to its DynamoDB attribute map representation.
    /// </summary>
    /// <returns>The DynamoDB attribute map representing the item.</returns>
    /// <remarks>
    /// By default, only includes the key attributes. Derived classes should override
    /// this method to include additional attributes specific to the item type.
    /// </remarks>
    public virtual Dictionary<string, AttributeValue> ToAttributeMap() => Key;

    #endregion
}

/// <summary>
/// Represents an item used for scanning the DynamoDB table.
/// </summary>
/// <remarks>
/// Provides support for building filter expressions for DynamoDB scan operations.
/// </remarks>
internal abstract class ScanItem
{
    #region Private Fields

    /// <summary>
    /// The attribute map used for scan filter conditions.
    /// </summary>
    private readonly Dictionary<string, AttributeValue> _attributeMap = [];

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the attribute map for this scan item.
    /// </summary>
    public IReadOnlyDictionary<string, AttributeValue> AttributeMap => _attributeMap;

    /// <summary>
    /// Gets the subject name prefix for filtering.
    /// </summary>
    /// <remarks>
    /// Used in the begins_with filter condition for the subjectName attribute.
    /// </remarks>
    public abstract string SubjectName { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a resourceName attribute to the filter conditions.
    /// </summary>
    /// <param name="resourceName">The resource name to filter by.</param>
    public void AddResourceName(
        string resourceName)
    {
        // Add the resource name to the attribute map.
        AddAttribute("resourceName", resourceName);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Adds an attribute to the filter conditions.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="attributeValue">The value of the attribute.</param>
    protected void AddAttribute(
        string attributeName,
        string attributeValue)
    {
        // Add the attribute name and value to the attribute map.
        _attributeMap.Add(attributeName, new AttributeValue(attributeValue));
    }

    #endregion
}

/// <summary>
/// Base repository class for DynamoDB data access.
/// </summary>
/// <typeparam name="TBaseItem">The type of item managed by this repository.</typeparam>
/// <remarks>
/// Provides CRUD operations and querying capabilities for a DynamoDB table.
/// Uses a composite key of resourceName and subjectName to identify items.
/// </remarks>
internal abstract class BaseRepository<TBaseItem>(
    AmazonDynamoDBClient client,
    string tableName)
    where TBaseItem : BaseItem
{
    #region Public Methods

    /// <summary>
    /// Creates the specified item in the DynamoDB table.
    /// </summary>
    /// <param name="item">The item to create.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    public Task CreateAsync(
        TBaseItem item,
        CancellationToken cancellationToken)
    {
        // Create the request.
        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = item.ToAttributeMap()
        };

        try
        {
            // Put the item.
            return client.PutItemAsync(request, cancellationToken);
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes the specified item from the DynamoDB table.
    /// </summary>
    /// <param name="item">The item to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    public async Task DeleteAsync(
        TBaseItem item,
        CancellationToken cancellationToken)
    {
        // Create the request.
        var request = new DeleteItemRequest
        {
            TableName = tableName,
            Key = item.Key
        };

        try
        {
            // Delete the item.
            await client.DeleteItemAsync(request, cancellationToken);
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Deletes a batch of items from the DynamoDB table.
    /// </summary>
    /// <param name="items">The array of items to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    /// <remarks>
    /// Uses DynamoDB's BatchWriteItem operation for better performance with multiple items.
    /// Handles continuation of unprocessed items automatically.
    /// </remarks>
    public async Task DeleteAsync(
        TBaseItem[] items,
        CancellationToken cancellationToken)
    {
        // If there are no items to delete, return.
        if (items.Length is 0) return;

        // Create the delete requests.
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
            // Delete until complete.
            while (batchWriteRequest.RequestItems.Count > 0)
            {
                // Delete.
                var response = await client.BatchWriteItemAsync(batchWriteRequest, cancellationToken);

                // Set the unprocessed items for the next iteration.
                batchWriteRequest.RequestItems = response.UnprocessedItems;
            }
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Gets the specified item from the DynamoDB table.
    /// </summary>
    /// <param name="item">The item to get (only key properties are used).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The item if found; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    public async Task<TBaseItem?> GetAsync(
        TBaseItem item,
        CancellationToken cancellationToken)
    {
        // Create the get request.
        var request = new GetItemRequest
        {
            TableName = tableName,
            Key = item.Key,
            ConsistentRead = true // Use consistent read for up-to-date data.
        };

        try
        {
            // Get the item.
            var response = await client.GetItemAsync(request, cancellationToken);

            // Convert the attribute map to the item.
            return BaseRepository<TBaseItem>.FromAttributeMap(response.Item);
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    /// <summary>
    /// Scans the DynamoDB table for items matching the specified criteria.
    /// </summary>
    /// <param name="scanItem">The scan criteria.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An array of matching items.</returns>
    /// <exception cref="HttpStatusCodeException">Thrown when a DynamoDB service error occurs.</exception>
    /// <remarks>
    /// Uses DynamoDB's Scan operation with filter expressions based on the attributes in the scanItem.
    /// Always includes a begins_with filter on the subjectName attribute.
    /// </remarks>
    public async Task<TBaseItem[]> ScanAsync(
        ScanItem scanItem,
        CancellationToken cancellationToken)
    {
        // Build the collection of filter conditions.
        // +1 to always include the subject name.
        var filterConditions = new List<string>(scanItem.AttributeMap.Count + 1)
        {
            "begins_with(subjectName, :subjectName)" // Always include a begins_with filter on the subjectName attribute.
        };

        // Create the expression attribute values.
        // Always include the subject name.
        var expressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":subjectName", new AttributeValue(scanItem.SubjectName) }
        };

        // Enumerate the attribute map.
        // Add the filter condition and expression attribute value.
        foreach (var kvp in scanItem.AttributeMap)
        {
            // Add the filter condition.
            // For example: "attributeName = :attributeName".
            filterConditions.Add(
                $"{kvp.Key} = :{kvp.Key}");

            // Add the expression attribute value.
            // For example: { ":attributeName", new AttributeValue(attributeValue) }.
            expressionAttributeValues.Add(
                $":{kvp.Key}",
                kvp.Value);
        }

        var request = new ScanRequest()
        {
            TableName = tableName,
            FilterExpression = string.Join(" AND ", filterConditions),
            ExpressionAttributeValues = expressionAttributeValues,
            ConsistentRead = true // Use consistent read for up-to-date data.
        };

        try
        {
            // Scan.
            var response = await client.ScanAsync(request, cancellationToken);

            // Convert the response to the items.
            var items = new List<TBaseItem>(response.Items.Count);
            foreach (var attributeMap in response.Items)
            {
                var responseItem = BaseRepository<TBaseItem>.FromAttributeMap(attributeMap);

                // Add the item to the list if it is not null.
                if (responseItem is not null) items.Add(responseItem);
            }

            // Return the array of items.
            return items.ToArray();
        }
        catch (AmazonDynamoDBException ex)
        {
            // Wrap and re-throw the exception with a more specific HTTP status code.
            throw new HttpStatusCodeException(HttpStatusCode.ServiceUnavailable, ex.Message);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Converts a DynamoDB attribute map to an item of type TBaseItem.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    /// <returns>The item if valid; otherwise, <see langword="null"/>.</returns>
    /// <remarks>
    /// Uses reflection to create an instance of TBaseItem from the attribute map.
    /// Validates that the subjectName in the item matches the one in the attribute map.
    /// </remarks>
    private static TBaseItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        // If the attribute map is null, return null.
        if (attributeMap is null) return default;

        try
        {
            // Create the item.
            var item = Activator.CreateInstance(typeof(TBaseItem), attributeMap) as TBaseItem;

            // Get the subject name.
            var subjectName = attributeMap["subjectName"].S;

            // Validate the subject name.
            // Return the item if the subject name matches; otherwise, return null.
            return string.Equals(item?.SubjectName, subjectName)
                ? item
                : default;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is KeyNotFoundException)
        {
            // If a KeyNotFoundException occurs during item creation, return null.
            return default;
        }
    }

    #endregion
}
