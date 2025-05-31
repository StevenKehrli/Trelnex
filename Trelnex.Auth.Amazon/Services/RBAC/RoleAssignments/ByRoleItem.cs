using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.RoleAssignments;

/// <summary>
/// Represents a role-principal assignment item optimized for querying principals by role.
/// </summary>
/// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
/// <param name="roleName">The name of the role, such as "rbac.create" or "rbac.read".</param>
/// <param name="principalId">The unique identifier of the principal, such as "arn:aws:iam::123456789012:user/john".</param>
/// <remarks>
/// This item uses the resource name as the partition key and the subject name format
/// "ROLE#{roleName}#PRINCIPAL#{principalId}" to enable efficient queries for finding
/// all principals assigned to a specific role.
/// </remarks>
internal class ByRoleItem(
    string resourceName,
    string roleName,
    string principalId)
    : BaseItem
{
    #region Public Static Methods

    /// <summary>
    /// Creates a ByRoleItem from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    public static ByRoleItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        if (attributeMap is null) return null;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_resourceName", out var resourceNameAttribute) is false) return null;
        var resourceName = resourceNameAttribute.S;

        // Extract the role name from the attribute map.
        if (attributeMap.TryGetValue("_roleName", out var roleNameAttribute) is false) return null;
        var roleName = roleNameAttribute.S;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_principalId", out var principalIdAttribute) is false) return null;
        var principalId = principalIdAttribute.S;

        // Create a new ByRoleItem instance with the extracted values.
        return new ByRoleItem(
            resourceName: resourceName,
            roleName: roleName,
            principalId: principalId);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => ItemName.FormatResource(resourceName);

    /// <inheritdoc/>
    public override string SubjectName => ItemName.FormatRoleAssignmentByPrincipal(
        roleName: roleName,
        principalId: principalId);

    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    public string ResourceName => resourceName;

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    /// <value>
    /// The role name, such as "rbac.create" or "rbac.read".
    /// </value>
    public string RoleName => roleName;

    /// <summary>
    /// Gets the unique identifier of the principal assigned to the role.
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
            .SubjectNameBeginsWith(ItemName.ROLEASSIGNMENT_MARKER)
            .Build();
    }

    /// <summary>
    /// Creates a query request for this item type.
    /// </summary>
    /// <param name="tableName">The name of the DynamoDB table to query.</param>
    /// <param name="resourceName">The resource name to filter by.</param>
    /// <param name="roleName">The role name to filter by.</param>
    /// <returns>
    /// A <see cref="QueryRequest"/> configured for querying this item type in DynamoDB.
    /// </returns>
    public static QueryRequest CreateQueryRequest(
        string tableName,
        string resourceName,
        string roleName)
    {
        var entityName = ItemName.FormatResource(resourceName);
        var subjectName = ItemName.FormatRoleAssignmentByPrincipal(roleName);

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
            { "_roleName", new AttributeValue(roleName) },
            { "_principalId", new AttributeValue(principalId) }
        };
    }

    #endregion
}
