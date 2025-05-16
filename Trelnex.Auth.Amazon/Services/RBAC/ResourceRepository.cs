using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Provides the subject name prefix for resources in DynamoDB.
/// </summary>
/// <remarks>
/// Used as part of the composite key in DynamoDB to identify resource entries.
/// The prefix allows for efficient filtering of resources during scan operations.
/// </remarks>
internal static class ResourceSubjectName
{
    /// <summary>
    /// Gets the subject name prefix for resource items.
    /// </summary>
    /// <returns>The string prefix used for resource subject names.</returns>
    public static string Get()
    {
        // Return the resource subject name prefix.
        return "RESOURCE#";
    }
}

/// <summary>
/// Represents a resource entity stored in DynamoDB.
/// </summary>
/// <remarks>
/// Resources are protected assets in the RBAC system that principals can be granted
/// access to through role assignments. Each resource is identified by its unique name.
/// </remarks>
internal class ResourceItem : BaseItem
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceItem"/> class.
    /// </summary>
    /// <param name="resourceName">The unique name of the resource.</param>
    public ResourceItem(
        string resourceName)
        : base(resourceName)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceItem"/> class from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map containing the resource data.</param>
    /// <exception cref="KeyNotFoundException">Thrown when required attributes are missing from the map.</exception>
    public ResourceItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the subject name prefix for this resource item.
    /// </summary>
    /// <remarks>
    /// This value is combined with the resourceName to form the composite key in DynamoDB.
    /// </remarks>
    public override string SubjectName => ResourceSubjectName.Get();

    #endregion
}

/// <summary>
/// Scan filter for querying resource items in DynamoDB.
/// </summary>
/// <remarks>
/// Used to filter DynamoDB scan operations to only include resource items.
/// Additional filter criteria can be added through the methods provided by the base class.
/// </remarks>
internal class ResourceScanItem : ScanItem
{
    #region Public Properties

    /// <summary>
    /// Gets the subject name prefix used to filter resources during scan operations.
    /// </summary>
    public override string SubjectName => ResourceSubjectName.Get();

    #endregion
}

/// <summary>
/// Repository for managing resource entities in DynamoDB.
/// </summary>
/// <remarks>
/// Provides CRUD operations for resources in the RBAC system. Resources are protected assets
/// that principals can be granted access to through role assignments. Each resource is identified
/// by its unique name and stored with the "RESOURCE#" subject name prefix.
///
/// This repository inherits all standard CRUD operations from <see cref="BaseRepository{TBaseItem}"/>
/// including:
/// <list type="bullet">
///   <item><description>CreateAsync - Create a new resource</description></item>
///   <item><description>GetAsync - Retrieve a resource by name</description></item>
///   <item><description>DeleteAsync - Remove a resource and its associated data</description></item>
///   <item><description>ScanAsync - Query for resources with optional filtering</description></item>
/// </list>
/// </remarks>
internal class ResourceRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<ResourceItem>(client, tableName);
