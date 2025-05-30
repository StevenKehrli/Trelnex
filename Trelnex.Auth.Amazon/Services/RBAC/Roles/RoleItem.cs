using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC.Roles;

/// <summary>
/// Represents a role item in the RBAC system.
/// </summary>
/// <param name="resourceName">The name of the resource that owns this role, such as "api://amazon.auth.trelnex.com".</param>
/// <param name="roleName">The name of the role, such as "rbac.create" or "rbac.read".</param>
/// <remarks>
/// A role defines a specific set of permissions and capabilities within a resource.
/// </remarks>
internal class RoleItem(
    string resourceName,
    string roleName)
    : BaseItem
{
    #region Public Static Methods

    /// <summary>
    /// Creates a RoleItem from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map.</param>
    public static RoleItem? FromAttributeMap(
        Dictionary<string, AttributeValue> attributeMap)
    {
        if (attributeMap is null) return null;

        // Extract the resource name from the attribute map.
        if (attributeMap.TryGetValue("_resourceName", out var resourceNameAttribute) is false) return null;
        var resourceName = resourceNameAttribute.S;

        // Extract the role name from the attribute map.
        if (attributeMap.TryGetValue("_roleName", out var roleNameAttribute) is false) return null;
        var roleName = roleNameAttribute.S;

        // Create a new RoleItem instance with the extracted values.
        return new RoleItem(
            resourceName: resourceName,
            roleName: roleName);
    }

    #endregion

    #region Public Properties

    /// <inheritdoc/>
    public override string EntityName => RBACFormatter.FormatResourceName(resourceName);

    /// <inheritdoc/>
    public override string SubjectName => RBACFormatter.FormatRoleName(roleName);

    /// <summary>
    /// Gets the name of the resource that owns this role.
    /// </summary>
    /// <value>
    /// The resource name, such as "api://amazon.auth.trelnex.com".
    /// </value>
    public string ResourceName => resourceName;

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    /// <value>
    /// The role name, such as "rbac.create", "rbac.read", "rbac.update", or "rbac.delete".
    /// </value>
    public string RoleName => roleName;

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
        var entityName = RBACFormatter.FormatResourceName(resourceName);

        return new QueryRequestBuilder()
            .WithTableName(tableName)
            .EntityNameEquals(entityName)
            .SubjectNameBeginsWith(RBACFormatter.ROLE_MARKER)
            .Build();
    }

    /// <inheritdoc/>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        return new Dictionary<string, AttributeValue>(Key)
        {
            { "_resourceName", new AttributeValue(resourceName) },
            { "_roleName", new AttributeValue(roleName) }
        };
    }

    #endregion
}
