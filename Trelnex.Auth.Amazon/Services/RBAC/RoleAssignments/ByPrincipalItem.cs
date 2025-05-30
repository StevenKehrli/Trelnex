using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.RoleAssignments;

/// <summary>
/// Represents a principal-role assignment item optimized for querying roles by principal.
/// </summary>
/// <param name="principalId">The unique identifier of the principal, such as "arn:aws:iam::123456789012:user/john".</param>
/// <param name="resourceName">The name of the resource, such as "api://amazon.auth.trelnex.com".</param>
/// <param name="roleName">The name of the role, such as "rbac.create" or "rbac.read".</param>
/// <remarks>
/// This item uses the principal ID as the partition key and the subject name format
/// "RESOURCE#{resourceName}#ROLE#{roleName}" to enable efficient queries for finding
/// all roles assigned to a specific principal.
/// </remarks>
internal class ByPrincipalItem(
    string principalId,
    string resourceName,
    string roleName)
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

        // Extract the role name from the attribute map.
        if (attributeMap.TryGetValue("_roleName", out var roleNameAttribute) is false) return null;
        var roleName = roleNameAttribute.S;

        // Create a new ByPrincipalItem instance with the extracted values.
        return new ByPrincipalItem(
            principalId: principalId,
            resourceName: resourceName,
            roleName: roleName);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => RBACFormatter.FormatPrincipalName(principalId);

    /// <inheritdoc/>
    public override string SubjectName => RBACFormatter.FormatRoleAssignmentNameByRole(
        resourceName: resourceName,
        roleName: roleName);

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
    /// Gets the name of the role assigned to the principal.
    /// </summary>
    /// <value>
    /// The role name, such as "rbac.create" or "rbac.read".
    /// </value>
    public string RoleName => roleName;

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
        var entityName = RBACFormatter.FormatPrincipalName(principalId);

        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(entityName)
            .SubjectNameBeginsWith(RBACFormatter.ROLEASSIGNMENT_MARKER)
            .Build();
    }

    public static QueryRequest CreateQueryRequest(
        string tableName,
        string principalId,
        string resourceName)
    {
        var entityName = RBACFormatter.FormatPrincipalName(principalId);
        var subjectNameBeginsWith = RBACFormatter.FormatRoleAssignmentNameByRole(resourceName);

        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(entityName)
            .SubjectNameBeginsWith(subjectNameBeginsWith)
            .Build();
    }

    /// <inheritdoc/>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        return new Dictionary<string, AttributeValue>(Key)
        {
            { "_resourceName", new AttributeValue(resourceName) },
            { "_principalId", new AttributeValue(principalId) },
            { "_roleName", new AttributeValue(roleName) }
        };
    }

    #endregion
}
