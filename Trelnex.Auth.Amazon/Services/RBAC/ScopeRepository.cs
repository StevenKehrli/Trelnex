using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Provides the subject name prefix for scopes in DynamoDB.
/// </summary>
/// <remarks>
/// Used as part of the composite key in DynamoDB to identify scope entries.
/// The prefix allows for efficient filtering of scopes during scan operations.
/// </remarks>
internal static class ScopeSubjectName
{
    /// <summary>
    /// Gets the subject name for scope items with an optional scope name.
    /// </summary>
    /// <param name="scopeName">The optional scope name to include in the subject name.</param>
    /// <returns>The subject name string, either the basic prefix or the prefix with the scope name.</returns>
    /// <remarks>
    /// When <paramref name="scopeName"/> is null, returns just the prefix for use in scans.
    /// When <paramref name="scopeName"/> is provided, returns the full subject name for a specific scope.
    /// </remarks>
    public static string Get(
        string? scopeName = null)
    {
        // Return the scope subject name with or without the scope name.
        return (scopeName is null)
            ? $"SCOPE#"
            : $"SCOPE#{scopeName}";
    }
}

/// <summary>
/// Represents a scope entity stored in DynamoDB.
/// </summary>
/// <remarks>
/// Scopes define authorization boundaries in the RBAC system that limit the context
/// in which roles apply. Each scope is associated with a resource and has a unique name.
/// Scopes are typically used to implement permission boundaries such as environments
/// (dev/test/prod) or geographical regions.
/// </remarks>
internal class ScopeItem : BaseItem
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeItem"/> class.
    /// </summary>
    /// <param name="resourceName">The name of the resource this scope applies to.</param>
    /// <param name="scopeName">The unique name of the scope.</param>
    public ScopeItem(
        string resourceName,
        string scopeName)
        : base(resourceName)
    {
        // Set the scope name.
        ScopeName = scopeName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeItem"/> class from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map containing the scope data.</param>
    /// <exception cref="KeyNotFoundException">Thrown when required attributes are missing from the map.</exception>
    /// <remarks>
    /// Used to reconstruct a scope item from its DynamoDB representation.
    /// </remarks>
    public ScopeItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
        // Get the scope name from the attribute map.
        ScopeName = attributeMap["scopeName"].S;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the unique name of the scope.
    /// </summary>
    /// <remarks>
    /// Scope names typically represent authorization boundaries such as environments
    /// (dev/test/prod) or geographical regions.
    /// </remarks>
    public string ScopeName { get; init; }

    /// <summary>
    /// Gets the subject name for this scope item.
    /// </summary>
    /// <remarks>
    /// Combines the scope prefix with the specific scope name to form part of the
    /// composite key in DynamoDB.
    /// </remarks>
    public override string SubjectName => ScopeSubjectName.Get(ScopeName);

    #endregion

    #region Public Methods

    /// <summary>
    /// Converts the scope to its DynamoDB attribute map representation.
    /// </summary>
    /// <returns>The DynamoDB attribute map representing the scope.</returns>
    /// <remarks>
    /// Extends the base attribute map with the scope-specific attributes.
    /// </remarks>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        // Get the base attribute map.
        var baseAttributeMap = base.ToAttributeMap();

        // Add the scope name to the attribute map.
        return new Dictionary<string, AttributeValue>(baseAttributeMap)
        {
            { "scopeName", new AttributeValue(ScopeName) }
        };
    }

    #endregion
}

/// <summary>
/// Scan filter for querying scope items in DynamoDB.
/// </summary>
/// <remarks>
/// Used to filter DynamoDB scan operations to only include scope items.
/// Additional filter criteria can be added through the methods provided by the base class.
/// </remarks>
internal class ScopeScanItem : ScanItem
{
    #region Public Properties

    /// <summary>
    /// Gets the subject name prefix used to filter scopes during scan operations.
    /// </summary>
    public override string SubjectName => ScopeSubjectName.Get();

    #endregion
}

/// <summary>
/// Repository for managing scope entities in DynamoDB.
/// </summary>
/// <remarks>
/// Provides CRUD operations for scopes in the RBAC system. Scopes define authorization
/// boundaries that limit the context in which roles apply. Each scope is identified by its
/// unique name within a resource and stored with the "SCOPE#{scopeName}" subject name format.
///
/// This repository inherits all standard CRUD operations from <see cref="BaseRepository{TBaseItem}"/>
/// including:
/// <list type="bullet">
///   <item><description>CreateAsync - Create a new scope for a resource</description></item>
///   <item><description>GetAsync - Retrieve a scope by resource and scope name</description></item>
///   <item><description>DeleteAsync - Remove a scope</description></item>
///   <item><description>ScanAsync - Query for scopes with optional filtering</description></item>
/// </list>
///
/// Scopes are typically used to implement permission boundaries such as environments
/// (dev/test/prod) or geographical regions.
/// </remarks>
internal class ScopeRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<ScopeItem>(client, tableName);
