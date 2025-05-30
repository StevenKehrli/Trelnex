using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.Resources;

/// <summary>
/// Represents a resource item in the RBAC system.
/// </summary>
/// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
/// <remarks>
/// A resource represents any protected asset that can be secured by the RBAC system.
/// </remarks>
internal class ResourceItem(
    string resourceName)
    : BaseItem
{
    #region Public Static Methods

    /// <summary>
    /// Creates a ResourceItem from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    public static ResourceItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        if (attributeMap is null) return null;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_resourceName", out var resourceNameAttribute) is false) return null;
        var resourceName = resourceNameAttribute.S;

        // Create a new ResourceItem instance with the extracted value.
        return new ResourceItem(resourceName: resourceName);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => RBACFormatter.RESOURCE_MARKER;

    /// <inheritdoc/>
    public override string SubjectName => RBACFormatter.FormatResourceName(resourceName);

    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    public string ResourceName => resourceName;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a query request for this item type.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table to query.</param>
    /// <returns>
    /// A <see cref="QueryRequest"/> configured for querying this item type in DynamoDB.
    /// </returns>
    public static QueryRequest CreateQueryRequest(
        string tableName)
    {
        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(RBACFormatter.RESOURCE_MARKER)
            .Build();
    }

    /// <inheritdoc/>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        return new Dictionary<string, AttributeValue>(Key)
        {
            { "_resourceName", new AttributeValue(resourceName) }
        };
    }

    #endregion
}
