using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.ScopeAssignments;

/// <summary>
/// Represents a scope-principal assignment item optimized for querying principals by scope.
/// </summary>
/// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
/// <param name="scopeName">The name of the scope, such as "rbac".</param>
/// <param name="principalId">The unique identifier of the principal, such as "arn:aws:iam::123456789012:user/john".</param>
/// <remarks>
/// This item uses the resource name as the partition key and the subject name format
/// "SCOPE#{scopeName}#PRINCIPAL#{principalId}" to enable efficient queries for finding
/// all principals assigned to a specific scope.
/// </remarks>
internal class ByScopeItem(
    string resourceName,
    string scopeName,
    string principalId)
    : BaseItem
{
    #region Public Static Methods

    /// <summary>
    /// Creates a ByScopeItem from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    public static ByScopeItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        if (attributeMap is null) return null;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_resourceName", out var resourceNameAttribute) is false) return null;
        var resourceName = resourceNameAttribute.S;

        // Extract the scope name from the attribute map.
        if (attributeMap.TryGetValue("_scopeName", out var scopeNameAttribute) is false) return null;
        var scopeName = scopeNameAttribute.S;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_principalId", out var principalIdAttribute) is false) return null;
        var principalId = principalIdAttribute.S;

        // Create a new ByScopeItem instance with the extracted values.
        return new ByScopeItem(
            resourceName: resourceName,
            scopeName: scopeName,
            principalId: principalId);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => ItemName.FormatResource(resourceName);

    /// <inheritdoc/>
    public override string SubjectName => ItemName.FormatScopeAssignmentByPrincipal(
        scopeName: scopeName,
        principalId: principalId);

    /// <summary>
    /// Gets the name of the resource.
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

    /// <summary>
    /// Gets the unique identifier of the principal assigned to the scope.
    /// </summary>
    /// <value>
    /// The principal identifier, such as "arn:aws:iam::123456789012:user/john".
    /// </value>
    public string PrincipalId => principalId;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a query request for this item type.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table to query.</param>
    /// <param name="resourceName">The resource name to filter by.</param>
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
            .SubjectNameBeginsWith(ItemName.SCOPEASSIGNMENT_MARKER)
            .Build();
    }

    /// <summary>
    /// Creates a query request for this item type.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table to query.</param>
    /// <param name="resourceName">The resource name to filter by.</param>
    /// <param name="scopeName">The scope name to filter by.</param>
    /// <returns>
    /// A <see cref="QueryRequest"/> configured for querying this item type in DynamoDB.
    /// </returns>
    public static QueryRequest CreateQueryRequest(
        string tableName,
        string resourceName,
        string scopeName)
    {
        var entityName = ItemName.FormatResource(resourceName);
        var subjectName = ItemName.FormatScopeAssignmentByPrincipal(scopeName);

        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(entityName)
            .SubjectNameBeginsWith(subjectName)
            .Build();
    }

    /// <inheritdoc/>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        return new Dictionary<string, AttributeValue>(Key)
        {
            { "_resourceName", new AttributeValue(resourceName) },
            { "_scopeName", new AttributeValue(scopeName) },
            { "_principalId", new AttributeValue(principalId) }
        };
    }

    #endregion
}
