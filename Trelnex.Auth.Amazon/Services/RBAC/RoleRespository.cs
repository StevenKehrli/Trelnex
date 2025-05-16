using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Provides the subject name prefix for roles in DynamoDB.
/// </summary>
/// <remarks>
/// Used as part of the composite key in DynamoDB to identify role entries.
/// The prefix allows for efficient filtering of roles during scan operations.
/// </remarks>
internal class RoleSubjectName
{
    /// <summary>
    /// Gets the subject name for role items with an optional role name.
    /// </summary>
    /// <param name="roleName">The optional role name to include in the subject name.</param>
    /// <returns>The subject name string, either the basic prefix or the prefix with the role name.</returns>
    /// <remarks>
    /// When <paramref name="roleName"/> is null, returns just the prefix for use in scans.
    /// When <paramref name="roleName"/> is provided, returns the full subject name for a specific role.
    /// </remarks>
    public static string Get(
        string? roleName = null)
    {
        // Return the role subject name with or without the role name.
        return (roleName is null)
            ? $"ROLE#"
            : $"ROLE#{roleName}";
    }
}

/// <summary>
/// Represents a role entity stored in DynamoDB.
/// </summary>
/// <remarks>
/// Roles define a set of permissions that can be granted to principals in the RBAC system.
/// Each role is associated with a resource and has a unique name within that resource context.
/// Roles are used to group permissions and simplify access management by assigning
/// them to principals instead of individual permissions.
/// </remarks>
internal class RoleItem : BaseItem
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleItem"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource this role applies to.</param>
    /// <param name="roleName">The unique name of the role.</param>
    public RoleItem(
        string resourceName,
        string roleName)
        : base(resourceName)
    {
        // Set the role name.
        RoleName = roleName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleItem"/> class from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map containing the role data.</param>
    /// <exception cref="KeyNotFoundException">Thrown when required attributes are missing from the map.</exception>
    /// <remarks>
    /// Used to reconstruct a role item from its DynamoDB representation.
    /// </remarks>
    public RoleItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
        // Get the role name from the attribute map.
        RoleName = attributeMap["roleName"].S;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the unique name of the role.
    /// </summary>
    /// <remarks>
    /// Role names typically describe the permissions granted, such as "Reader", "Editor",
    /// or "Administrator".
    /// </remarks>
    public string RoleName { get; init; }

    /// <summary>
    /// Gets the subject name for this role item.
    /// </summary>
    /// <remarks>
    /// Combines the role prefix with the specific role name to form part of the
    /// composite key in DynamoDB.
    /// </remarks>
    public override string SubjectName => RoleSubjectName.Get(RoleName);

    #endregion

    #region Public Methods

    /// <summary>
    /// Converts the role to its DynamoDB attribute map representation.
    /// </summary>
    /// <returns>The DynamoDB attribute map representing the role.</returns>
    /// <remarks>
    /// Extends the base attribute map with the role-specific attributes.
    /// </remarks>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        // Get the base attribute map.
        var baseAttributeMap = base.ToAttributeMap();

        // Add the role name to the attribute map.
        return new Dictionary<string, AttributeValue>(baseAttributeMap)
        {
            { "roleName", new AttributeValue(RoleName) }
        };
    }

    #endregion
}

/// <summary>
/// Scan filter for querying role items in DynamoDB.
/// </summary>
/// <remarks>
/// Used to filter DynamoDB scan operations to only include role items.
/// Additional filter criteria can be added through the methods provided by the base class.
/// </remarks>
internal class RoleScanItem : ScanItem
{
    #region Public Properties

    /// <summary>
    /// Gets the subject name prefix used to filter roles during scan operations.
    /// </summary>
    public override string SubjectName => RoleSubjectName.Get();

    #endregion
}

/// <summary>
/// Repository for managing role entities in DynamoDB.
/// </summary>
/// <remarks>
/// Provides CRUD operations for roles in the RBAC system. Roles define sets of permissions
/// that can be granted to principals for specific resources. Each role is identified by its
/// unique name within a resource context and stored with the "ROLE#{roleName}" subject name format.
///
/// This repository inherits all standard CRUD operations from <see cref="BaseRepository{TBaseItem}"/>
/// including:
/// <list type="bullet">
///   <item><description>CreateAsync - Create a new role for a resource</description></item>
///   <item><description>GetAsync - Retrieve a role by resource and role name</description></item>
///   <item><description>DeleteAsync - Remove a role</description></item>
///   <item><description>ScanAsync - Query for roles with optional filtering</description></item>
/// </list>
///
/// Roles are typically used to group permissions like "Reader", "Editor", or "Administrator"
/// to simplify access management.
/// </remarks>
internal class RoleRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<RoleItem>(client, tableName);
