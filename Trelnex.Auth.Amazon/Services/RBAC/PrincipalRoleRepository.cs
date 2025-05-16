using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Trelnex.Auth.Amazon.Services.RBAC;

/// <summary>
/// Provides utilities for generating consistent subject name patterns for principal role mappings in DynamoDB.
/// </summary>
/// <remarks>
/// This class helps generate hierarchical key patterns for efficient querying of principal-role relationships.
/// It follows a pattern of "PRINCIPAL#[principalId]#ROLE#[roleName]" to enable prefix-based queries.
/// </remarks>
internal static class PrincipalRoleSubjectName
{
    /// <summary>
    /// Generates a subject name for principal-role relationships based on provided parameters.
    /// </summary>
    /// <param name="principalId">The principal identifier (optional).</param>
    /// <param name="roleName">The role name (optional).</param>
    /// <returns>A formatted subject name string for DynamoDB querying.</returns>
    /// <remarks>
    /// The method returns different key prefixes based on provided parameters:
    /// - With no parameters: "PRINCIPAL#" (prefix for all principals)
    /// - With principalId only: "PRINCIPAL#{principalId}#ROLE#" (prefix for all roles of a specific principal)
    /// - With both principalId and roleName: "PRINCIPAL#{principalId}#ROLE#{roleName}" (specific principal-role mapping)
    ///
    /// This hierarchical approach supports efficient querying at different levels of specificity.
    /// </remarks>
    public static string Get(
        string? principalId = null,
        string? roleName = null)
    {
        // Return different key prefixes based on provided parameters.
        return (principalId is null)
            ? $"PRINCIPAL#"
            : (roleName is null)
                ? $"PRINCIPAL#{principalId}#ROLE#"
                : $"PRINCIPAL#{principalId}#ROLE#{roleName}";
    }
}

/// <summary>
/// Represents a principal-role mapping item in the RBAC system.
/// </summary>
/// <remarks>
/// This class models the relationship between a principal (user or service) and
/// a role for a specific resource. It serves as the core data structure for
/// storing role assignments in the RBAC system.
/// </remarks>
internal class PrincipalRoleItem : BaseItem
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalRoleItem"/> class.
    /// </summary>
    /// <param name="principalId">The unique identifier of the principal (user or service).</param>
    /// <param name="resourceName">The name of the resource to which the role applies.</param>
    /// <param name="roleName">The name of the role being assigned to the principal.</param>
    /// <remarks>
    /// Creates a new principal-role mapping that grants a specific role on a resource to a principal.
    /// </remarks>
    public PrincipalRoleItem(
        string principalId,
        string resourceName,
        string roleName)
        : base(resourceName)
    {
        // Set the principal ID and role name.
        PrincipalId = principalId;
        RoleName = roleName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PrincipalRoleItem"/> class from a DynamoDB attribute map.
    /// </summary>
    /// <param name="attributeMap">The DynamoDB attribute map containing the item data.</param>
    /// <exception cref="KeyNotFoundException">Thrown when required attributes are missing from the map.</exception>
    /// <remarks>
    /// This constructor deserializes a principal-role mapping from its DynamoDB representation.
    /// </remarks>
    public PrincipalRoleItem(
        Dictionary<string, AttributeValue> attributeMap)
        : base(attributeMap)
    {
        // Get the principal ID and role name from the attribute map.
        PrincipalId = attributeMap["principalId"].S;
        RoleName = attributeMap["roleName"].S;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the unique identifier of the principal (user or service).
    /// </summary>
    /// <remarks>
    /// This identifier is typically an AWS ARN for the principal, but can be
    /// any string that uniquely identifies the user or service.
    /// </remarks>
    public string PrincipalId { get; init; }

    /// <summary>
    /// Gets the name of the role assigned to the principal.
    /// </summary>
    /// <remarks>
    /// This corresponds to a role defined in the RBAC system and grants
    /// specific permissions on the associated resource.
    /// </remarks>
    public string RoleName { get; init; }

    /// <summary>
    /// Gets the subject name for this principal-role mapping.
    /// </summary>
    /// <remarks>
    /// The subject name follows a specific pattern to enable efficient querying
    /// of principal-role relationships in DynamoDB.
    /// </remarks>
    public override string SubjectName => PrincipalRoleSubjectName.Get(PrincipalId, RoleName);

    #endregion

    #region Public Methods

    /// <summary>
    /// Converts this principal-role mapping to its DynamoDB attribute map representation.
    /// </summary>
    /// <returns>The DynamoDB attribute map representing this principal-role mapping.</returns>
    /// <remarks>
    /// This method extends the base implementation to include the principalId and roleName attributes.
    /// </remarks>
    public override Dictionary<string, AttributeValue> ToAttributeMap()
    {
        // Get the base attributes from the parent class (resourceName and subjectName).
        var baseAttributeMap = base.ToAttributeMap();

        // Add principal-role specific attributes.
        return new Dictionary<string, AttributeValue>(baseAttributeMap)
        {
            { "principalId", new AttributeValue(PrincipalId) },
            { "roleName", new AttributeValue(RoleName) }
        };
    }

    #endregion
}

/// <summary>
/// Represents criteria for scanning principal-role mappings in DynamoDB.
/// </summary>
/// <remarks>
/// This class helps build filter conditions for querying principal-role mappings
/// based on principal ID and/or role name.
/// </remarks>
internal class PrincipalRoleScanItem : ScanItem
{
    #region Public Properties

    /// <summary>
    /// Gets the subject name prefix for filtering principal-role mappings.
    /// </summary>
    /// <remarks>
    /// This property dynamically generates a subject name prefix based on the
    /// current filter attributes (principalId and/or roleName).
    /// </remarks>
    public override string SubjectName => GetSubjectName();

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a principal ID filter condition for scanning.
    /// </summary>
    /// <param name="principalId">The principal ID to filter by.</param>
    /// <remarks>
    /// This method adds a condition to filter by a specific principal.
    /// </remarks>
    public void AddPrincipalId(
        string principalId)
    {
        // Add the principal ID to the attribute map.
        AddAttribute("principalId", principalId);
    }

    /// <summary>
    /// Adds a role name filter condition for scanning.
    /// </summary>
    /// <param name="roleName">The role name to filter by.</param>
    /// <remarks>
    /// This method adds a condition to filter by a specific role.
    /// </remarks>
    public void AddRoleName(
        string roleName)
    {
        // Add the role name to the attribute map.
        AddAttribute("roleName", roleName);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Generates a subject name based on the current filter attributes.
    /// </summary>
    /// <returns>A subject name prefix for filtering.</returns>
    /// <remarks>
    /// This method extracts the principalId and roleName from the current
    /// attribute map and generates an appropriate subject name prefix.
    /// </remarks>
    private string GetSubjectName()
    {
        // Extract the principal ID and role name from the attribute map (if present).
        var principalId = AttributeMap.GetValueOrDefault("principalId")?.S;
        var roleName = AttributeMap.GetValueOrDefault("roleName")?.S;

        // Generate a subject name prefix based on the available attributes.
        return PrincipalRoleSubjectName.Get(principalId, roleName);
    }

    #endregion
}

/// <summary>
/// Provides data access operations for principal-role mappings in the RBAC system.
/// </summary>
/// <remarks>
/// This repository manages the relationships between principals (users or services)
/// and roles for specific resources. It implements CRUD operations for principal-role
/// mappings stored in DynamoDB.
///
/// The repository inherits from <see cref="BaseRepository{T}"/> to leverage common
/// data access functionality while specializing for principal-role mappings.
/// </remarks>
internal class PrincipalRoleRepository(
    AmazonDynamoDBClient client,
    string tableName)
    : BaseRepository<PrincipalRoleItem>(client, tableName);
