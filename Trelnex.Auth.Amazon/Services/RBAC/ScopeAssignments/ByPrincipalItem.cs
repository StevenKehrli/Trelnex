using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.ScopeAssignments;

/// <summary>
/// Represents a principal-scope assignment item optimized for querying scopes by principal.
/// </summary>
/// <param name="principalId">The unique identifier of the principal, such as "arn:aws:iam::123456789012:user/john".</param>
/// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
/// <param name="scopeName">The name of the scope, such as "rbac".</param>
/// <remarks>
/// This item uses the principal ID as the partition key and the subject name format
/// "RESOURCE#{resourceName}#SCOPE#{scopeName}" to enable efficient queries for finding
/// all scopes assigned to a specific principal.
/// </remarks>
internal class ByPrincipalItem(
    string principalId,
    string resourceName,
    string scopeName)
    : BaseItem
{
    #region Public Static Methods

    /// <summary>
    /// Creates a ByPrincipalItem from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    public static ByPrincipalItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        if (attributeMap is null) return null;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_principalId", out var principalIdAttribute) is false) return null;
        var principalId = principalIdAttribute.S;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_resourceName", out var resourceNameAttribute) is false) return null;
        var resourceName = resourceNameAttribute.S;

        // Extract the scope name from the attribute map.
        if (attributeMap.TryGetValue("_scopeName", out var scopeNameAttribute) is false) return null;
        var scopeName = scopeNameAttribute.S;

        // Create a new ByPrincipalItem instance with the extracted values.
        return new ByPrincipalItem(
            principalId: principalId,
            resourceName: resourceName,
            scopeName: scopeName);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => ItemName.FormatPrincipal(principalId);

    /// <inheritdoc/>
    public override string SubjectName => ItemName.FormatScopeAssignmentByScope(
        resourceName: resourceName,
        scopeName: scopeName);

    /// <summary>
    /// Gets the unique identifier of the principal.
    /// </summary>
    /// <value>
    /// The principal identifier, such as "arn:aws:iam::123456789012:user/john".
    /// </value>
    public string PrincipalId => principalId;

    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    public string ResourceName => resourceName;

    /// <summary>
    /// Gets the name of the scope assigned to the principal.
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
    /// <param name="principalId">The principal ID to filter by.</param>
    /// <returns>
    /// A <see cref="QueryRequest"/> configured for querying this item type in DynamoDB.
    /// </returns>
    public static QueryRequest CreateQueryRequest(
        string tableName,
        string principalId)
    {
        var entityName = ItemName.FormatPrincipal(principalId);

        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(entityName)
            .SubjectNameBeginsWith(ItemName.SCOPEASSIGNMENT_MARKER)
            .Build();
    }

    public static QueryRequest CreateQueryRequest(
        string tableName,
        string principalId,
        string resourceName)
    {
        var entityName = ItemName.FormatPrincipal(principalId);
        var subjectName = ItemName.FormatScopeAssignmentByScope(resourceName);

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
            { "_principalId", new AttributeValue(principalId) },
            { "_resourceName", new AttributeValue(resourceName) },
            { "_scopeName", new AttributeValue(scopeName) }
        };
    }

    #endregion
}
