using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Represents a base item in the RBAC system.
/// </summary>
/// <remarks>
/// All RBAC entities (resources, scopes, roles, and assignments) inherit from this class
/// to provide consistent DynamoDB storage operations using the entityName and subjectName keys.
/// </remarks>
internal abstract class BaseItem
{
    /// <summary>
    /// Gets the entity name, used as the partition key in DynamoDB.
    /// </summary>
    /// <value>
    /// The entity name, which can be
    /// "RESOURCE#" for all resources,
    /// "RESOURCE#{resourceName}" for a specific resource,
    /// "PRINCIPAL#{principalId}" for principal-specific assignment.
    /// </value>
    public abstract string EntityName { get; }

    /// <summary>
    /// Gets the subject name, used as the sort key in DynamoDB.
    /// </summary>
    /// <value>
    /// The subject name with entity-specific prefixes, like
    /// "RESOURCE#" for a specific resource,
    /// "SCOPE#{name}" for a specific scope of a resource,
    /// "ROLE#{name}" for a specific role of a resource,
    /// "PRINCIPAL#{id}#..." for a principal-specific assignment.
    /// </value>
    public abstract string SubjectName { get; }

    /// <summary>
    /// Converts the item to its DynamoDB attribute map representation.
    /// </summary>
    /// <returns>A dictionary containing the DynamoDB attribute values for this item.</returns>
    public abstract Dictionary<string, AttributeValue> ToAttributeMap();

    /// <summary>
    /// Gets the DynamoDB key for this item.
    /// </summary>
    /// <value>
    /// A dictionary containing the entityName and subjectName as the composite key.
    /// </value>
    public Dictionary<string, AttributeValue> Key => new()
    {
        { "entityName", new AttributeValue(EntityName) },
        { "subjectName", new AttributeValue(SubjectName) }
    };
}
