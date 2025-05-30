using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.Scopes;

/// <summary>
/// Represents a scope item in the RBAC system.
/// </summary>
/// <param name="resourceName">The name of the resource that owns this scope, such as "api://amazon.auth.trelnex.com".</param>
/// <param name="scopeName">The name of the scope, such as "rbac".</param>
/// <remarks>
/// A scope defines a specific permission boundary within a resource.
/// </remarks>
internal class ScopeItem(
    string resourceName,
    string scopeName)
    : BaseItem
{
    #region Public Static Methods

    /// <summary>
    /// Creates a ScopeItem from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    public static ScopeItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        if (attributeMap is null) return null;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_resourceName", out var resourceNameAttribute) is false) return null;
        var resourceName = resourceNameAttribute.S;

        // Extract the scope name from the attribute map.
        if (attributeMap.TryGetValue("_scopeName", out var scopeNameAttribute) is false) return null;
        var scopeName = scopeNameAttribute.S;

        // Create a new ScopeItem instance with the extracted values.
        return new ScopeItem(
            resourceName: resourceName,
            scopeName: scopeName);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => ItemName.FormatResource(resourceName);

    /// <inheritdoc/>
    public override string SubjectName => ItemName.FormatScope(scopeName);

    /// <summary>
    /// Gets the name of the resource that owns this scope.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    public string ResourceName => resourceName;

    /// <summary>
    /// Gets the name of the scope.
    /// </summary>
    /// <value>
    /// The scope name, such as "rbac".
    /// </value>
    public string ScopeName => scopeName;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a query request for this item type.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table to query.</param>
    /// <param name="resourceName">The name of the resource to filter by.</param>
    /// <returns>
    /// A <see cref="QueryRequest"/> configured for querying this item type in DynamoDB.
    /// </returns>
    public static QueryRequest CreateQueryRequest(
        string tableName,
        string resourceName)
    {
        var entityName = ItemName.FormatResource(resourceName);

        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(entityName)
            .SubjectNameBeginsWith(ItemName.SCOPE_MARKER)
            .Build();
    }

    /// <inheritdoc/>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        return new Dictionary<string, AttributeValue>(Key)
        {
            { "_resourceName", new AttributeValue(resourceName) },
            { "_scopeName", new AttributeValue(scopeName) }
        };
    }

    #endregion
}
